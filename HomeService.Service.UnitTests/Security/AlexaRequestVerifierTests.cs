using System.Net;
using System.Text;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Transversal.Security.Alexa;
using Microsoft.AspNetCore.Http;

namespace HomeService.Service.UnitTests.Security
{
    // Sin red: el HttpClient va contra un handler falso. Lo que importa es que las
    // URLs que no cumplen las reglas de Amazon se rechacen antes de descargar nada.
    public class AlexaRequestVerifierTests
    {
        private static readonly byte[] _body = Encoding.UTF8.GetBytes("{\"request\":{\"timestamp\":\"2026-10-30T10:00:00Z\"}}");

        #region Verify_MissingHeaders_ReturnsFalseWithoutDownloading
        [Theory]
        [InlineData("", "firma")]
        [InlineData("https://s3.amazonaws.com/echo.api/echo-api-cert.pem", "")]
        [InlineData("", "")]
        public async Task Verify_MissingHeaders_ReturnsFalseWithoutDownloading(string certUrl, string signature)
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK);
            AlexaRequestVerifier verifier = new AlexaRequestVerifier(new HttpClient(handler));

            bool isValid = await verifier.Verify(certUrl, signature, _body);

            Assert.False(isValid);
            Assert.Empty(handler.Requests);
        }
        #endregion

        #region Verify_InvalidCertificateUrl_ReturnsFalseWithoutDownloading
        [Theory]
        [InlineData("http://s3.amazonaws.com/echo.api/echo-api-cert.pem")]
        [InlineData("https://s3.amazonaws.com:563/echo.api/echo-api-cert.pem")]
        [InlineData("https://notamazon.com/echo.api/echo-api-cert.pem")]
        [InlineData("https://s3.amazonaws.com.evil.com/echo.api/echo-api-cert.pem")]
        [InlineData("https://s3.amazonaws.com/EcHo.aPi.extra/echo-api-cert.pem")]
        [InlineData("https://s3.amazonaws.com/invalid.path/echo-api-cert.pem")]
        [InlineData("https://s3.amazonaws.com/echo.api/../invalid.path/echo-api-cert.pem")]
        [InlineData("https://s3.amazonaws.com/echo.api/%2e%2e/invalid.path/echo-api-cert.pem")]
        [InlineData("no es una url")]
        [InlineData("/echo.api/echo-api-cert.pem")]
        public async Task Verify_InvalidCertificateUrl_ReturnsFalseWithoutDownloading(string certUrl)
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK);
            AlexaRequestVerifier verifier = new AlexaRequestVerifier(new HttpClient(handler));

            bool isValid = await verifier.Verify(certUrl, "ZmlybWE=", _body);

            Assert.False(isValid);
            Assert.Empty(handler.Requests);
        }
        #endregion

        #region Verify_ValidCertificateUrl_TriesToDownloadCertificate
        // Las URLs validas de los ejemplos de Amazon: el host y la ruta no distinguen
        // mayusculas y el puerto 443 explicito vale.
        [Theory]
        [InlineData("https://s3.amazonaws.com/echo.api/echo-api-cert.pem")]
        [InlineData("https://s3.amazonaws.com:443/echo.api/echo-api-cert.pem")]
        [InlineData("https://s3.amazonaws.com/EcHo.aPi/echo-api-cert.pem")]
        [InlineData("https://S3.AMAZONAWS.COM/echo.api/echo-api-cert.pem")]
        [InlineData("https://s3.amazonaws.com/invalid.path/../echo.api/echo-api-cert.pem")]
        public async Task Verify_ValidCertificateUrl_TriesToDownloadCertificate(string certUrl)
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.NotFound);
            AlexaRequestVerifier verifier = new AlexaRequestVerifier(new HttpClient(handler));

            bool isValid = await verifier.Verify(certUrl, "ZmlybWE=", _body);

            Assert.False(isValid);
            Assert.Single(handler.Requests);
        }
        #endregion

        #region Verify_DownloadIsNotACertificate_ReturnsFalse
        [Theory]
        [InlineData("")]
        [InlineData("esto no es un pem")]
        [InlineData("-----BEGIN CERTIFICATE-----\nnobase64\n-----END CERTIFICATE-----")]
        public async Task Verify_DownloadIsNotACertificate_ReturnsFalse(string downloadedBody)
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, downloadedBody);
            AlexaRequestVerifier verifier = new AlexaRequestVerifier(new HttpClient(handler));

            bool isValid = await verifier.Verify("https://s3.amazonaws.com/echo.api/not-a-cert-" + Guid.NewGuid() + ".pem", "ZmlybWE=", _body);

            Assert.False(isValid);
        }
        #endregion

        #region Verify_NetworkError_ReturnsFalseWithoutThrowing
        [Fact]
        public async Task Verify_NetworkError_ReturnsFalseWithoutThrowing()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Throwing(new HttpRequestException("sin red"));
            AlexaRequestVerifier verifier = new AlexaRequestVerifier(new HttpClient(handler));

            bool isValid = await verifier.Verify("https://s3.amazonaws.com/echo.api/echo-api-cert.pem", "ZmlybWE=", _body);

            Assert.False(isValid);
        }
        #endregion

        #region AmazonApprove_WithoutSignatureHeaders_ReturnsFalseAndRewindsBody
        [Fact]
        public async Task AmazonApprove_WithoutSignatureHeaders_ReturnsFalseAndRewindsBody()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK);
            AlexaRequestVerifier verifier = new AlexaRequestVerifier(new HttpClient(handler));

            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(_body);

            bool isApproved = await verifier.AmazonApprove(httpContext.Request);

            Assert.False(isApproved);
            Assert.Empty(handler.Requests);
            Assert.Equal(0, httpContext.Request.Body.Position);

            using StreamReader reader = new StreamReader(httpContext.Request.Body);
            Assert.Equal(Encoding.UTF8.GetString(_body), await reader.ReadToEndAsync());
        }
        #endregion

        #region AmazonApprove_ReadsSignatureHeaders
        [Fact]
        public async Task AmazonApprove_ReadsSignatureHeaders()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.NotFound);
            AlexaRequestVerifier verifier = new AlexaRequestVerifier(new HttpClient(handler));

            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(_body);
            httpContext.Request.Headers["SignatureCertChainUrl"] = "https://s3.amazonaws.com/echo.api/echo-api-cert.pem";
            httpContext.Request.Headers["Signature"] = "ZmlybWE=";

            bool isApproved = await verifier.AmazonApprove(httpContext.Request);

            Assert.False(isApproved);
            HttpRequestMessage request = Assert.Single(handler.Requests);
            Assert.Equal("https://s3.amazonaws.com/echo.api/echo-api-cert.pem", request.RequestUri!.ToString());
        }
        #endregion
    }
}
