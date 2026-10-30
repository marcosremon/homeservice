using Microsoft.AspNetCore.Mvc;

namespace HomeService.Service.UnitTests.TestSetup
{
    public static class ResultAssert
    {
        #region Json
        // Los controladores devuelven siempre un JsonResult de ControllerUtils: se
        // comprueba el codigo HTTP y se devuelve el cuerpo ya tipado.
        public static TContent Json<TContent>(IActionResult actionResult, int expectedStatusCode)
        {
            JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
            Assert.Equal(expectedStatusCode, jsonResult.StatusCode);

            return Assert.IsType<TContent>(jsonResult.Value);
        }
        #endregion
    }
}
