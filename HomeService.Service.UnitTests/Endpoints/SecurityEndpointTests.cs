using System.Net;
using System.Text.Json;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.GetComputerStatus;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Transversal.Wrapper.Base;
using Moq;

namespace HomeService.Service.UnitTests.Endpoints
{
    // ApiKeyAuth en todos los endpoints REST, sobre el pipeline de verdad.
    public class SecurityEndpointTests : EndpointTestBase
    {
        public SecurityEndpointTests(HomeServiceWebApplicationFactory factory) : base(factory) { }

        public static TheoryData<string, string> RestEndpoints => new TheoryData<string, string>
        {
            { "GET", "/api/change-computer-status/turn-on-computer" },
            { "GET", "/api/change-computer-status/turn-off-computer" },
            { "GET", "/api/change-computer-status/get-computer-status" },
            { "POST", "/api/sensor/presence-sensor/create-presence-sensor" },
            { "PATCH", "/api/sensor/presence-sensor/patch-presence-sensor-data" },
            { "POST", "/api/sensors/rain-sensor/create-rain-sensor" },
            { "PATCH", "/api/sensors/rain-sensor/patch-rain-sensor" },
            { "POST", "/api/roomba/create-roomba" },
            { "PATCH", "/api/roomba/patch-roomba-state" },
            { "POST", "/api/sensors/temperature-sensor/create-temperature-sensor" },
            { "PATCH", "/api/sensors/temperature-sensor/patch-temperature-sensor" },
        };

        #region RestEndpoint_WithoutApiKey_Returns401
        [Theory]
        [MemberData(nameof(RestEndpoints))]
        public async Task RestEndpoint_WithoutApiKey_Returns401(string method, string url)
        {
            HttpResponseMessage response = await Send(new HttpMethod(method), url, method == "GET" ? null : "{}", apiKey: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            JsonElement json = await ReadJson(response);
            Assert.Equal("Unauthorized", json.GetProperty("detail").GetString());
        }
        #endregion

        #region RestEndpoint_WithWrongApiKey_Returns401
        [Theory]
        [MemberData(nameof(RestEndpoints))]
        public async Task RestEndpoint_WithWrongApiKey_Returns401(string method, string url)
        {
            HttpResponseMessage response = await Send(new HttpMethod(method), url, method == "GET" ? null : "{}", apiKey: "clave-mala");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        #endregion

        #region RestEndpoint_ApiKeyIsCaseSensitive
        [Fact]
        public async Task RestEndpoint_ApiKeyIsCaseSensitive()
        {
            HttpResponseMessage response = await Send(HttpMethod.Get, "/api/change-computer-status/get-computer-status", apiKey: TestEnvironment.InternalApiKey.ToUpperInvariant());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        #endregion

        #region RestEndpoint_ApiKeyWithExtraCharacters_Returns401
        [Fact]
        public async Task RestEndpoint_ApiKeyWithExtraCharacters_Returns401()
        {
            HttpResponseMessage response = await Send(HttpMethod.Get, "/api/change-computer-status/get-computer-status", apiKey: TestEnvironment.InternalApiKey + "x");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        #endregion

        #region RestEndpoint_WithoutApiKey_NeverReachesApplication
        [Fact]
        public async Task RestEndpoint_WithoutApiKey_NeverReachesApplication()
        {
            await Send(HttpMethod.Get, "/api/change-computer-status/turn-on-computer", apiKey: null);

            Factory.ChangeComputerStatusApplication.Verify(application => application.TurnOnComputer(), Times.Never);
        }
        #endregion

        #region RestEndpoint_WithDebugBypassKey_SkipsApiKey
        [Fact]
        public async Task RestEndpoint_WithDebugBypassKey_SkipsApiKey()
        {
            Factory.ChangeComputerStatusApplication.Setup(application => application.GetComputerStatus())
                .ReturnsAsync(new GetComputerStatusResponse { ResponseCode = ResponseCodes.OK, IsSuccess = true, ComputerStatus = true, Message = "ON" });

            HttpResponseMessage response = await Send(HttpMethod.Get, "/api/change-computer-status/get-computer-status", apiKey: null, debugKey: TestEnvironment.DebugBypassKey);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        #endregion

        #region RestEndpoint_WithWrongDebugKey_Returns401
        [Fact]
        public async Task RestEndpoint_WithWrongDebugKey_Returns401()
        {
            HttpResponseMessage response = await Send(HttpMethod.Get, "/api/change-computer-status/get-computer-status", apiKey: null, debugKey: "mala");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        #endregion

        #region RouteWithoutApiPrefix_Returns404
        [Theory]
        [InlineData("/change-computer-status/get-computer-status")]
        [InlineData("/roomba/patch-roomba-state")]
        [InlineData("/alexa")]
        public async Task RouteWithoutApiPrefix_Returns404(string url)
        {
            HttpResponseMessage response = await Send(HttpMethod.Get, url);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        #endregion

        #region UnknownRoute_Returns404
        [Theory]
        [InlineData("/api/sensors/presence-sensor/create-presence-sensor")]
        [InlineData("/api/sensor/rain-sensor/create-rain-sensor")]
        [InlineData("/api/lights")]
        public async Task UnknownRoute_Returns404(string url)
        {
            HttpResponseMessage response = await Send(HttpMethod.Post, url, "{}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        #endregion

        #region WrongHttpMethod_Returns405
        [Theory]
        [InlineData("POST", "/api/change-computer-status/turn-on-computer")]
        [InlineData("GET", "/api/sensor/presence-sensor/create-presence-sensor")]
        [InlineData("POST", "/api/sensor/presence-sensor/patch-presence-sensor-data")]
        [InlineData("PUT", "/api/roomba/patch-roomba-state")]
        [InlineData("GET", "/api/alexa")]
        [InlineData("DELETE", "/api/sensors/rain-sensor/patch-rain-sensor")]
        public async Task WrongHttpMethod_Returns405(string method, string url)
        {
            HttpResponseMessage response = await Send(new HttpMethod(method), url, method == "GET" ? null : "{}");

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }
        #endregion
    }
}
