using System.Net;

namespace HomeService.Service.UnitTests.TestSetup
{
    // Sustituye la red en los servicios que usan HttpClient: guarda lo que se envia y
    // devuelve la respuesta que diga el test.
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> RequestBodies { get; } = [];

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        #region Returning
        public static FakeHttpMessageHandler Returning(HttpStatusCode statusCode, string body = "") =>
            new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body) }));
        #endregion

        #region Throwing
        public static FakeHttpMessageHandler Throwing(Exception exception) =>
            new FakeHttpMessageHandler((_, _) => Task.FromException<HttpResponseMessage>(exception));
        #endregion

        #region SendAsync
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

            return await _responder(request, cancellationToken);
        }
        #endregion
    }

    public class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        #region CreateClient
        public HttpClient CreateClient(string name) => new HttpClient(_handler, disposeHandler: false);
        #endregion
    }
}
