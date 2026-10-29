using HomeService.Application.DataTransferObject.Notification.SendNotification;
using HomeService.Application.Interface.Service;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Base;
using System.Net;
using System.Text;

namespace HomeService.Infraestructure.Gateway
{
    // Push al movil a traves de ntfy.
    public class NotificationService : INotificationService
    {
        private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(5);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Settings _settings;

        public NotificationService(IHttpClientFactory httpClientFactory, Settings settings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings;
        }

        #region SendNotification
        public async Task<SendNotificationResponse> SendNotification(SendNotificationRequest sendNotificationRequest)
        {
            SendNotificationResponse sendNotificationResponse = new SendNotificationResponse();

            try
            {
                if (GeneralUtils.IsNullOrEmpty(_settings.NtfyTopic))
                {
                    sendNotificationResponse.ResponseCode = ResponseCodes.OK;
                    sendNotificationResponse.IsSuccess = false;
                    sendNotificationResponse.Message = "Notifications are disabled: no ntfy topic configured.";
                }
                else
                {
                    using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_settings.NtfyBaseUrl.TrimEnd('/')}/{_settings.NtfyTopic}");
                    httpRequestMessage.Content = new StringContent(sendNotificationRequest.Message, Encoding.UTF8);
                    httpRequestMessage.Headers.Add("Title", sendNotificationRequest.Title);

                    if (!GeneralUtils.IsNullOrEmpty(sendNotificationRequest.Tags))
                        httpRequestMessage.Headers.Add("Tags", sendNotificationRequest.Tags);

                    using CancellationTokenSource timeout = new CancellationTokenSource(_requestTimeout);
                    HttpClient httpClient = _httpClientFactory.CreateClient();
                    using HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, timeout.Token);

                    if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                    {
                        string body = await httpResponseMessage.Content.ReadAsStringAsync();

                        sendNotificationResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                        sendNotificationResponse.IsSuccess = false;
                        sendNotificationResponse.Message = $"Ntfy responded with HTTP {(int)httpResponseMessage.StatusCode}: {body}";
                    }
                    else
                    {
                        sendNotificationResponse.ResponseCode = ResponseCodes.OK;
                        sendNotificationResponse.IsSuccess = true;
                        sendNotificationResponse.Message = "Notification sent successfully.";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                sendNotificationResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                sendNotificationResponse.IsSuccess = false;
                sendNotificationResponse.Message = "Timeout on NotificationService -> SendNotification";
            }
            catch (Exception ex)
            {
                sendNotificationResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                sendNotificationResponse.IsSuccess = false;
                sendNotificationResponse.Message = $"Unexpected error on NotificationService -> SendNotification: {ex.Message}";
            }

            return sendNotificationResponse;
        }
        #endregion
    }
}
