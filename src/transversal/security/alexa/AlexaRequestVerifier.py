"""
Verifica que una peticion viene realmente de Amazon Alexa siguiendo los pasos que
exige Amazon: cabeceras presentes, URL del certificado valida, descarga y cacheo
del certificado, cadena y SAN validos, firma del cuerpo y timestamp dentro de
tolerancia (anti-replay).
"""

import asyncio
import base64
import json
import logging
from datetime import datetime, timedelta, timezone
from urllib.parse import urlparse

import certifi
import httpx
from cryptography import x509
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.asymmetric import padding, rsa
from cryptography.x509.verification import PolicyBuilder, Store
from fastapi import Request

from application.interface.security.IAlexaRequestVerifier import IAlexaRequestVerifier

_logger = logging.getLogger(__name__)

_AMAZON_CERT_DOMAIN = "s3.amazonaws.com"
_AMAZON_CERT_PATH = "/echo.api/"
_AMAZON_SAN_ENTRY = "echo-api.amazon.com"
_MAX_TIMESTAMP_TOLERANCE_SECONDS = 150

_certCache: dict[str, x509.Certificate] = {}
_cacheLock = asyncio.Lock()

class AlexaRequestVerifier(IAlexaRequestVerifier):
    def __init__(self, httpClient: httpx.AsyncClient):
        self._httpClient = httpClient

    # region amazon_approve
    async def AmazonApprove(self, request: Request) -> bool:
        """Recibe la peticion entrante y devuelve True si es legitima de Amazon."""
        try:
            # Starlette cachea el cuerpo, asi que el endpoint puede volver a leerlo
            # despues. No hace falta rebobinar el stream como en C#.
            rawBody: bytes = await request.body()

            signatureCertChainUrl = request.headers.get("SignatureCertChainUrl", "")
            signature = request.headers.get("Signature", "")

            return await self.verify(signatureCertChainUrl, signature, rawBody)
        except Exception as ex:
            _logger.warning("AlexaRequestVerifier -> amazon_approve -> %s", ex)
            return False
    # endregion

    # region verify
    async def verify(self, signatureCertChainUrl: str, signature: str, rawBody: bytes) -> bool:
        """Verifica la firma del request. True solo si el request es legitimo de Amazon."""
        try:
            # 1. Validar que las cabeceras llegaron
            if not signatureCertChainUrl or not signature:
                return False

            # 2. Validar que la URL del certificado cumple los requisitos de Amazon
            if not self._isValidCertificateUrl(signatureCertChainUrl):
                return False

            # 3 y 4. Obtener (o cachear) el certificado, validando cadena, vigencia y SAN
            certificate = await self._getCertificate(signatureCertChainUrl)
            if certificate is None:
                return False

            # 5. Verificar la firma criptografica del cuerpo
            if not self._verifySignature(certificate, rawBody, signature):
                return False

            # 6. Verificar timestamp (evita replay attacks)
            if not self._isTimestampValid(rawBody):
                return False

            return True
        except Exception as ex:
            _logger.warning("AlexaRequestVerifier -> verify -> %s", ex)
            return False
    # endregion

    # region _is_valid_certificate_url
    @staticmethod
    def _isValidCertificateUrl(url: str) -> bool:
        """PASO 2: Amazon exige que la URL cumpla condiciones muy especificas."""
        try:
            uri = urlparse(url)
            port = uri.port
        except ValueError:
            return False

        if uri.scheme != "https":
            return False

        if (uri.hostname or "").lower() != _AMAZON_CERT_DOMAIN:
            return False

        if port not in (None, 443):
            return False

        # normpath colapsa los /../ que permitirian escapar de /echo.api/
        normalizedPath = _normalizePath(uri.path)
        if not normalizedPath.lower().startswith(_AMAZON_CERT_PATH):
            return False

        return True
    # endregion

    # region _get_certificate
    async def _getCertificate(self, certUrl: str) -> x509.Certificate | None:
        """PASOS 3 y 4: descargar, validar y cachear el certificado."""
        async with _cacheLock:
            cached = _certCache.get(certUrl)
            if cached is not None:
                # El cacheo es por URL, pero el certificado caduca: revalidamos vigencia.
                if self._isStillValid(cached):
                    return cached
                del _certCache[certUrl]

            response = await self._httpClient.get(certUrl)
            response.raise_for_status()

            # El fichero de Amazon es un bundle PEM: hoja + intermedios.
            collection = x509.load_pem_x509_certificates(response.content)
            if not collection:
                return None

            leaf = collection[0]
            intermediates = collection[1:]

            # build_server_verifier hace de una vez lo que en C# son X509Chain +
            # comprobacion de vigencia + busqueda del SAN echo-api.amazon.com.
            store = Store(x509.load_pem_x509_certificates(certifi.contents().encode("utf-8")))
            verifier = PolicyBuilder().store(store).build_server_verifier(x509.DNSName(_AMAZON_SAN_ENTRY))

            try:
                verifier.verify(leaf, intermediates)
            except Exception as ex:
                _logger.warning("AlexaRequestVerifier -> cadena de certificado no valida: %s -> %s", certUrl, ex)
                return None

            _certCache[certUrl] = leaf
            return leaf
    # endregion

    # region _is_still_valid
    @staticmethod
    def _isStillValid(certificate: x509.Certificate) -> bool:
        now = datetime.now(timezone.utc)
        return certificate.not_valid_before_utc <= now <= certificate.not_valid_after_utc
    # endregion

    # region _verify_signature
    @staticmethod
    def _verifySignature(certificate: x509.Certificate, rawBody: bytes, base64Signature: str) -> bool:
        """PASO 5: verificar firma criptografica (RSA + SHA1 + PKCS#1 v1.5)."""
        try:
            publicKey = certificate.public_key()
            if not isinstance(publicKey, rsa.RSAPublicKey):
                return False

            signatureBytes = base64.b64decode(base64Signature, validate=True)

            # verify lanza InvalidSignature si no cuadra; no devuelve bool.
            publicKey.verify(signatureBytes, rawBody, padding.PKCS1v15(), hashes.SHA1())
            return True
        except Exception:
            return False
    # endregion

    # region _is_timestamp_valid
    @staticmethod
    def _isTimestampValid(rawBody: bytes) -> bool:
        """PASO 6: verificar timestamp para evitar replay attacks."""
        try:
            payload = json.loads(rawBody)
            timestampStr = payload.get("request", {}).get("timestamp", "")
            if not timestampStr:
                return False

            timestamp = datetime.fromisoformat(timestampStr)
            if timestamp.tzinfo is None:
                timestamp = timestamp.replace(tzinfo=timezone.utc)

            difference = abs(datetime.now(timezone.utc) - timestamp)
            return difference <= timedelta(seconds=_MAX_TIMESTAMP_TOLERANCE_SECONDS)
        except Exception:
            return False
    # endregion

def _normalizePath(path: str) -> str:
    """Colapsa /./ y /../ sin tocar el sistema de ficheros (no usa os.path)."""
    segments: list[str] = []
    for segment in path.split("/"):
        if segment == "" or segment == ".":
            continue
        if segment == "..":
            if segments:
                segments.pop()
            continue
        segments.append(segment)

    normalized = "/" + "/".join(segments)
    if path.endswith("/") and not normalized.endswith("/"):
        normalized += "/"

    return normalized