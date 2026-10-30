using HomeService.Application.Interface.Security;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Transversal.Security.Alexa;
using HomeService.Transversal.Security.Filter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HomeService.Service.UnitTests.Security
{
    public class AlexaAuthTests
    {
        private readonly Mock<IAlexaRequestVerifier> _alexaRequestVerifier = new Mock<IAlexaRequestVerifier>();

        #region _createContext
        private AuthorizationFilterContext _createContext(string? debugKey = null)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(_alexaRequestVerifier.Object);

            DefaultHttpContext httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

            if (debugKey is not null)
                httpContext.Request.Headers[DebugBypass.HeaderName] = debugKey;

            return new AuthorizationFilterContext(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>());
        }
        #endregion

        #region OnAuthorizationAsync_ApprovedByAmazon_LetsRequestThrough
        [Fact]
        public async Task OnAuthorizationAsync_ApprovedByAmazon_LetsRequestThrough()
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            AuthorizationFilterContext context = _createContext();

            await new AlexaAuth().OnAuthorizationAsync(context);

            Assert.Null(context.Result);
        }
        #endregion

        #region OnAuthorizationAsync_RejectedByAmazon_Returns401
        [Fact]
        public async Task OnAuthorizationAsync_RejectedByAmazon_Returns401()
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(false);
            AuthorizationFilterContext context = _createContext();

            await new AlexaAuth().OnAuthorizationAsync(context);

            ObjectResult objectResult = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        }
        #endregion

        #region OnAuthorizationAsync_DebugBypass_SkipsVerifier
        [Fact]
        public async Task OnAuthorizationAsync_DebugBypass_SkipsVerifier()
        {
            AuthorizationFilterContext context = _createContext(TestEnvironment.DebugBypassKey);

            await new AlexaAuth().OnAuthorizationAsync(context);

            Assert.Null(context.Result);
            _alexaRequestVerifier.Verify(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>()), Times.Never);
        }
        #endregion

        #region GetAlexaRequestVerifier_ReturnsRealVerifier
        [Fact]
        public void GetAlexaRequestVerifier_ReturnsRealVerifier()
        {
            Assert.IsType<AlexaRequestVerifier>(AlexaAuth.GetAlexaRequestVerifier());
        }
        #endregion
    }
}
