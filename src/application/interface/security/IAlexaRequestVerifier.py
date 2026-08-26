from abc import ABC, abstractmethod

from fastapi import Request

class IAlexaRequestVerifier(ABC):
    @abstractmethod
    async def AmazonApprove(self, request: Request) -> bool: ...

    @abstractmethod
    async def Verify(self, signatureCertChainUrl: str, signature: str, rawBody: bytes) -> bool: ...