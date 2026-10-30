using HomeService.Service.UnitTests.TestSetup;
using HomeService.Transversal.Security.Filter;

namespace HomeService.Service.UnitTests.Security
{
    public class DebugBypassTests
    {
        #region HeaderName_IsXDebugKey
        [Fact]
        public void HeaderName_IsXDebugKey()
        {
            Assert.Equal("X-Debug-Key", DebugBypass.HeaderName);
        }
        #endregion

        #region IsRequested_ConfiguredKey_ReturnsTrue
        [Fact]
        public void IsRequested_ConfiguredKey_ReturnsTrue()
        {
            Assert.True(DebugBypass.IsRequested(TestEnvironment.DebugBypassKey));
        }
        #endregion

        #region IsRequested_AnythingElse_ReturnsFalse
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("mala")]
        [InlineData(TestEnvironment.InternalApiKey)]
        [InlineData(TestEnvironment.DebugBypassKey + "x")]
        public void IsRequested_AnythingElse_ReturnsFalse(string? debugKey)
        {
            Assert.False(DebugBypass.IsRequested(debugKey));
        }
        #endregion

        #region IsRequested_IsCaseSensitive
        [Fact]
        public void IsRequested_IsCaseSensitive()
        {
            Assert.False(DebugBypass.IsRequested(TestEnvironment.DebugBypassKey.ToUpperInvariant()));
        }
        #endregion
    }
}
