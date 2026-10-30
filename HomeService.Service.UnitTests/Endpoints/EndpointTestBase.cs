using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HomeService.Service.UnitTests.TestSetup;

namespace HomeService.Service.UnitTests.Endpoints
{
    // Los tests de endpoints comparten una WebApi en memoria por clase; xUnit crea una
    // instancia de la clase por test, asi que el constructor limpia los mocks.
    public abstract class EndpointTestBase : IClassFixture<HomeServiceWebApplicationFactory>
    {
        protected const string ApiKeyHeaderName = "X-Api-Key";
        protected const string DebugKeyHeaderName = "X-Debug-Key";

        protected HomeServiceWebApplicationFactory Factory { get; }
        protected HttpClient Client { get; }

        protected EndpointTestBase(HomeServiceWebApplicationFactory factory)
        {
            Factory = factory;
            Factory.ResetMocks();
            Client = factory.CreateClient();
        }

        #region Send
        protected async Task<HttpResponseMessage> Send(HttpMethod method, string url, string? jsonBody = null, string? apiKey = TestEnvironment.InternalApiKey, string? debugKey = null)
        {
            using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, url);

            if (jsonBody is not null)
                httpRequestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            if (apiKey is not null)
                httpRequestMessage.Headers.Add(ApiKeyHeaderName, apiKey);

            if (debugKey is not null)
                httpRequestMessage.Headers.Add(DebugKeyHeaderName, debugKey);

            return await Client.SendAsync(httpRequestMessage);
        }
        #endregion

        #region ReadJson
        protected static async Task<JsonElement> ReadJson(HttpResponseMessage httpResponseMessage)
        {
            string body = await httpResponseMessage.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(body);

            return document.RootElement.Clone();
        }
        #endregion

        #region AssertJsonContentType
        protected static void AssertJsonContentType(HttpResponseMessage httpResponseMessage)
        {
            MediaTypeHeaderValue? contentType = httpResponseMessage.Content.Headers.ContentType;
            Assert.NotNull(contentType);
            Assert.Equal("application/json", contentType.MediaType);
        }
        #endregion
    }
}
