using System.Net;
using HomeService.Application.DataTransferObject.Notification.SendNotification;
using HomeService.Infraestructure.Gateway;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Service.UnitTests.Gateway
{
    public class NotificationServiceTests
    {
        private static readonly SendNotificationRequest _request = new SendNotificationRequest
        {
            Title = "Esta lloviendo",
            Message = "El sensor lluvia de terraza ha detectado lluvia.",
            Tags = "rain_cloud",
        };

        #region _create
        private static NotificationService _create(FakeHttpMessageHandler handler, string topic = "mi-topic", string baseUrl = "https://ntfy.example") =>
            new NotificationService(new FakeHttpClientFactory(handler), new Settings { NtfyTopic = topic, NtfyBaseUrl = baseUrl });
        #endregion

        #region SendNotification_WithoutTopic_IsDisabledAndDoesNotCallNtfy
        [Fact]
        public async Task SendNotification_WithoutTopic_IsDisabledAndDoesNotCallNtfy()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK);

            SendNotificationResponse response = await _create(handler, topic: string.Empty).SendNotification(_request);

            Assert.Equal(ResponseCodes.OK, response.ResponseCode);
            Assert.False(response.IsSuccess);
            Assert.Empty(handler.Requests);
        }
        #endregion

        #region SendNotification_PostsMessageWithTitleAndTags
        [Fact]
        public async Task SendNotification_PostsMessageWithTitleAndTags()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK);

            SendNotificationResponse response = await _create(handler).SendNotification(_request);

            Assert.True(response.IsSuccess);
            Assert.Equal(ResponseCodes.OK, response.ResponseCode);

            HttpRequestMessage request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://ntfy.example/mi-topic", request.RequestUri!.ToString());
            Assert.Equal("Esta lloviendo", request.Headers.GetValues("Title").Single());
            Assert.Equal("rain_cloud", request.Headers.GetValues("Tags").Single());
            Assert.Equal(_request.Message, handler.RequestBodies.Single());
        }
        #endregion

        #region SendNotification_BaseUrlWithTrailingSlash_IsTrimmed
        [Fact]
        public async Task SendNotification_BaseUrlWithTrailingSlash_IsTrimmed()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK);

            await _create(handler, baseUrl: "https://ntfy.example/").SendNotification(_request);

            Assert.Equal("https://ntfy.example/mi-topic", handler.Requests.Single().RequestUri!.ToString());
        }
        #endregion

        #region SendNotification_WithoutTags_DoesNotSendTagsHeader
        [Fact]
        public async Task SendNotification_WithoutTags_DoesNotSendTagsHeader()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK);

            await _create(handler).SendNotification(new SendNotificationRequest { Title = "t", Message = "m" });

            Assert.False(handler.Requests.Single().Headers.Contains("Tags"));
        }
        #endregion

        #region SendNotification_NtfyError_ReturnsUnexpectedErrorWithBody
        [Theory]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.Created)]
        public async Task SendNotification_NtfyError_ReturnsUnexpectedErrorWithBody(HttpStatusCode statusCode)
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(statusCode, "limite");

            SendNotificationResponse response = await _create(handler).SendNotification(_request);

            Assert.False(response.IsSuccess);
            Assert.Equal(ResponseCodes.UNEXPECTED_ERROR, response.ResponseCode);
            Assert.Equal($"Ntfy responded with HTTP {(int)statusCode}: limite", response.Message);
        }
        #endregion

        #region SendNotification_Timeout_ReturnsTimeoutMessage
        [Fact]
        public async Task SendNotification_Timeout_ReturnsTimeoutMessage()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Throwing(new TaskCanceledException("timeout"));

            SendNotificationResponse response = await _create(handler).SendNotification(_request);

            Assert.False(response.IsSuccess);
            Assert.Equal("Timeout on NotificationService -> SendNotification", response.Message);
        }
        #endregion

        #region SendNotification_NetworkError_ReturnsOnlyExceptionMessage
        [Fact]
        public async Task SendNotification_NetworkError_ReturnsOnlyExceptionMessage()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Throwing(new HttpRequestException("sin red"));

            SendNotificationResponse response = await _create(handler).SendNotification(_request);

            Assert.False(response.IsSuccess);
            Assert.Equal(ResponseCodes.UNEXPECTED_ERROR, response.ResponseCode);
            Assert.Equal("Unexpected error on NotificationService -> SendNotification: sin red", response.Message);
        }
        #endregion
    }
}
