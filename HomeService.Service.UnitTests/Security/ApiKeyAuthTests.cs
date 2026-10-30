using HomeService.Service.UnitTests.TestSetup;
using HomeService.Transversal.Security.Filter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace HomeService.Service.UnitTests.Security
{
    public class ApiKeyAuthTests
    {
        #region _createContext
        private static AuthorizationFilterContext _createContext(string? apiKey = null, string? debugKey = null)
        {
            DefaultHttpContext httpContext = new DefaultHttpContext();

            if (apiKey is not null)
                httpContext.Request.Headers["X-Api-Key"] = apiKey;

            if (debugKey is not null)
                httpContext.Request.Headers[DebugBypass.HeaderName] = debugKey;

            ActionContext actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }
        #endregion

        #region OnAuthorization_CorrectApiKey_LetsRequestThrough
        [Fact]
        public void OnAuthorization_CorrectApiKey_LetsRequestThrough()
        {
            AuthorizationFilterContext context = _createContext(TestEnvironment.InternalApiKey);

            new ApiKeyAuth().OnAuthorization(context);

            Assert.Null(context.Result);
        }
        #endregion

        #region OnAuthorization_WrongOrMissingApiKey_Returns401
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("clave-mala")]
        [InlineData(" " + TestEnvironment.InternalApiKey)]
        [InlineData(TestEnvironment.InternalApiKey + " ")]
        public void OnAuthorization_WrongOrMissingApiKey_Returns401(string? apiKey)
        {
            AuthorizationFilterContext context = _createContext(apiKey);

            new ApiKeyAuth().OnAuthorization(context);

            ObjectResult objectResult = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        }
        #endregion

        #region OnAuthorization_DebugBypassKey_LetsRequestThroughWithoutApiKey
        [Fact]
        public void OnAuthorization_DebugBypassKey_LetsRequestThroughWithoutApiKey()
        {
            AuthorizationFilterContext context = _createContext(debugKey: TestEnvironment.DebugBypassKey);

            new ApiKeyAuth().OnAuthorization(context);

            Assert.Null(context.Result);
        }
        #endregion

        #region OnAuthorization_WrongDebugKey_FallsBackToApiKey
        [Fact]
        public void OnAuthorization_WrongDebugKey_FallsBackToApiKey()
        {
            AuthorizationFilterContext withoutApiKey = _createContext(debugKey: "mala");
            AuthorizationFilterContext withApiKey = _createContext(TestEnvironment.InternalApiKey, "mala");

            new ApiKeyAuth().OnAuthorization(withoutApiKey);
            new ApiKeyAuth().OnAuthorization(withApiKey);

            Assert.IsType<ObjectResult>(withoutApiKey.Result);
            Assert.Null(withApiKey.Result);
        }
        #endregion
    }
}
