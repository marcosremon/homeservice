using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;
using HomeService.Application.Interface.Security;
using HomeService.Transversal.Common.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeService.Transversal.Security.Alexa;

// Verifica que una peticion viene realmente de Amazon alexa siguiendo los pasos que
// exige Amazon: cabeceras presentes, URL del certificado valida, descarga y cacheo
// del certificado, cadena y SAN validos, firma del cuerpo y timestamp dentro de
// tolerancia (anti-replay).
public class AlexaRequestVerifier : IAlexaRequestVerifier
{
    private const string AmazonCertDomain = "s3.amazonaws.com";
    private const string AmazonCertPath = "/echo.api/";
    private const string AmazonSanEntry = "echo-api.amazon.com";
    private const int MaxTimestampToleranceSeconds = 150;

    private static readonly Dictionary<string, X509Certificate2> CertCache = [];
    private static readonly SemaphoreSlim CacheLock = new SemaphoreSlim(1, 1);

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public AlexaRequestVerifier(HttpClient httpClient, ILogger<AlexaRequestVerifier>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<AlexaRequestVerifier>.Instance;
    }

    #region AmazonApprove
    // Recibe la peticion entrante y devuelve true si es legitima de Amazon.
    public async Task<bool> AmazonApprove(HttpRequest request)
    {
        try
        {
            // Hay que rebobinar el stream: el endpoint vuelve a leer el cuerpo despues.
            request.EnableBuffering();
            request.Body.Position = 0;

            byte[] rawBody;
            using (MemoryStream buffer = new MemoryStream())
            {
                await request.Body.CopyToAsync(buffer);
                rawBody = buffer.ToArray();
            }

            request.Body.Position = 0;

            string signatureCertChainUrl = request.Headers["SignatureCertChainUrl"].ToString();
            string signature = request.Headers["Signature"].ToString();

            return await Verify(signatureCertChainUrl, signature, rawBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AlexaRequestVerifier -> AmazonApprove -> {Message}", ex.Message);
            return false;
        }
    }
    #endregion

    #region Verify
    // Verifica la firma del request. true solo si el request es legitimo de Amazon.
    public async Task<bool> Verify(string signatureCertChainUrl, string signature, byte[] rawBody)
    {
        try
        {
            // 1. Validar que las cabeceras llegaron
            if (GeneralUtils.IsNullOrEmpty(signatureCertChainUrl) || GeneralUtils.IsNullOrEmpty(signature))
                return false;

            // 2. Validar que la URL del certificado cumple los requisitos de Amazon
            if (!IsValidCertificateUrl(signatureCertChainUrl))
                return false;

            // 3 y 4. Obtener (o cachear) el certificado, validando cadena, vigencia y SAN
            X509Certificate2? certificate = await GetCertificate(signatureCertChainUrl);
            if (certificate is null)
                return false;

            // 5. Verificar la firma criptografica del cuerpo
            if (!VerifySignature(certificate, rawBody, signature))
                return false;

            // 6. Verificar timestamp (evita replay attacks)
            if (!IsTimestampValid(rawBody))
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AlexaRequestVerifier -> Verify -> {Message}", ex.Message);
            return false;
        }
    }
    #endregion

    #region IsValidCertificateUrl
    // PASO 2: Amazon exige que la URL cumpla condiciones muy especificas.
    private static bool IsValidCertificateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttps)
            return false;

        if (!string.Equals(uri.Host, AmazonCertDomain, StringComparison.OrdinalIgnoreCase))
            return false;

        if (uri.Port != 443)
            return false;

        // NormalizePath colapsa los /../ que permitirian escapar de /echo.api/
        return NormalizePath(uri.AbsolutePath).StartsWith(AmazonCertPath, StringComparison.OrdinalIgnoreCase);
    }
    #endregion

    #region GetCertificate
    // PASOS 3 y 4: descargar, validar y cachear el certificado.
    private async Task<X509Certificate2?> GetCertificate(string certUrl)
    {
        await CacheLock.WaitAsync();

        try
        {
            if (CertCache.TryGetValue(certUrl, out X509Certificate2? cached))
            {
                // El cacheo es por URL, pero el certificado caduca: revalidamos vigencia.
                if (IsStillValid(cached))
                    return cached;

                CertCache.Remove(certUrl);
                cached.Dispose();
            }

            using HttpResponseMessage response = await _httpClient.GetAsync(certUrl);
            response.EnsureSuccessStatusCode();

            // El fichero de Amazon es un bundle PEM: hoja + intermedios.
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.ImportFromPem(await response.Content.ReadAsStringAsync());

            if (collection.Count == 0)
                return null;

            X509Certificate2 leaf = collection[0];

            // X509Chain cubre la cadena y la vigencia de una vez; MatchesHostname busca
            // el SAN echo-api.amazon.com, como el build_server_verifier de python.
            using X509Chain chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.System;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            for (int index = 1; index < collection.Count; index++)
                chain.ChainPolicy.ExtraStore.Add(collection[index]);

            if (!chain.Build(leaf) || !leaf.MatchesHostname(AmazonSanEntry))
            {
                _logger.LogWarning("AlexaRequestVerifier -> cadena de certificado no valida: {CertUrl}", certUrl);
                return null;
            }

            CertCache[certUrl] = leaf;

            return leaf;
        }
        finally
        {
            CacheLock.Release();
        }
    }
    #endregion

    #region IsStillValid
    private static bool IsStillValid(X509Certificate2 certificate)
    {
        DateTime now = DateTime.UtcNow;

        return certificate.NotBefore.ToUniversalTime() <= now && now <= certificate.NotAfter.ToUniversalTime();
    }
    #endregion

    #region VerifySignature
    // PASO 5: verificar firma criptografica (RSA + SHA1 + PKCS#1 v1.5).
    private static bool VerifySignature(X509Certificate2 certificate, byte[] rawBody, string base64Signature)
    {
        try
        {
            using RSA? publicKey = certificate.GetRSAPublicKey();
            if (publicKey is null)
                return false;

            byte[] signatureBytes = Convert.FromBase64String(base64Signature);

            return publicKey.VerifyData(rawBody, signatureBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
    #endregion

    #region IsTimestampValid
    // PASO 6: verificar timestamp para evitar replay attacks.
    private static bool IsTimestampValid(byte[] rawBody)
    {
        try
        {
            JsonNode? root = JsonNode.Parse(rawBody);
            JsonNode? timestampNode = root?["request"]?["timestamp"];

            if (timestampNode?.GetValueKind() != JsonValueKind.String)
                return false;

            if (!DateTimeOffset.TryParse(
                    timestampNode.GetValue<string>(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset timestamp))
                return false;

            TimeSpan difference = (DateTimeOffset.UtcNow - timestamp).Duration();

            return difference <= TimeSpan.FromSeconds(MaxTimestampToleranceSeconds);
        }
        catch
        {
            return false;
        }
    }
    #endregion

    #region NormalizePath
    // Colapsa /./ y /../ sin tocar el sistema de ficheros.
    private static string NormalizePath(string path)
    {
        List<string> segments = [];

        foreach (string segment in path.Split('/'))
        {
            if (segment.Length == 0 || segment == ".")
                continue;

            if (segment == "..")
            {
                if (segments.Count > 0)
                    segments.RemoveAt(segments.Count - 1);

                continue;
            }

            segments.Add(segment);
        }

        string normalized = "/" + string.Join('/', segments);

        if (path.EndsWith('/') && !normalized.EndsWith('/'))
            normalized += "/";

        return normalized;
    }
    #endregion
}
