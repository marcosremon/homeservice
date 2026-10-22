using Microsoft.AspNetCore.Http;

namespace HomeService.Application.Interface.Security;

public interface IAlexaRequestVerifier
{
    Task<bool> AmazonApprove(HttpRequest request);

    Task<bool> Verify(string signatureCertChainUrl, string signature, byte[] rawBody);
}
