namespace HomeService.Application.Interface.Service;

public interface IMqttService
{
    Task Publish(string topic, string payload, bool retain = true);
}
