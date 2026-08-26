from abc import ABC, abstractmethod

from fastapi import Request

class IAlexaRequestVerifier(ABC):
    @abstractmethod
    async def amazon_approve(self, request: Request) -> bool: ...

    @abstractmethod
    async def verify(self, signature_cert_chain_url: str, signature: str, raw_body: bytes) -> bool: ...