namespace HomeService.Domain.Model.Enum.Alexa;

// Los nombres son los que manda Amazon tal cual: la busqueda tiene que ser exacta,
// de ahi que no sigan la convencion PascalCase de C#.
public enum IntentName
{
    ConversationIntent = 0,
    roomba_order_ = 1,
    computer_status_order_ = 2,
    light_order_ = 3,
    temperature_sensor_order_ = 4
}
