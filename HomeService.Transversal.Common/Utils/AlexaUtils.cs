using System.Text.Json;
using System.Text.RegularExpressions;
using HomeService.Domain.Model.Enum.Alexa;
using HomeService.Transversal.JsonInterchange.Alexa;
using HomeService.Transversal.Wrapper.Alexa.AlexaRequest;
using HomeService.Transversal.Wrapper.Alexa.AlexaResponse;
using Microsoft.AspNetCore.Http;

namespace HomeService.Transversal.Common.Utils;

public static class AlexaUtils
{
    private static readonly Regex DaysPattern = new(@"(\d+)D", RegexOptions.Compiled);
    private static readonly Regex HoursPattern = new(@"(\d+)H", RegexOptions.Compiled);
    private static readonly Regex MinutesPattern = new(@"(\d+)M", RegexOptions.Compiled);
    private static readonly Regex SecondsPattern = new(@"(\d+)S", RegexOptions.Compiled);

    #region ParseAlexaDuration
    // Duracion ISO-8601 de alexa (P1DT2H30M) a TimeSpan, con su limitacion de siempre:
    // la "M" de meses y la de minutos comparten patron, asi que "P2M" se lee como 2
    // minutos. Como alexa solo manda duraciones cortas en los slots AMAZON.DURATION,
    // no molesta.
    public static TimeSpan ParseAlexaDuration(string isoDuration)
    {
        TimeSpan total = TimeSpan.Zero;

        if (GeneralUtils.IsNullOrEmpty(isoDuration) || !isoDuration.StartsWith('P'))
        {
            Console.WriteLine($"[ParseAlexaDuration] Formato invalido: '{isoDuration}'");
            return total;
        }

        Match daysMatch = DaysPattern.Match(isoDuration);
        if (daysMatch.Success)
            total += TimeSpan.FromDays(int.Parse(daysMatch.Groups[1].Value));

        Match hoursMatch = HoursPattern.Match(isoDuration);
        if (hoursMatch.Success)
            total += TimeSpan.FromHours(int.Parse(hoursMatch.Groups[1].Value));

        Match minutesMatch = MinutesPattern.Match(isoDuration);
        if (minutesMatch.Success)
            total += TimeSpan.FromMinutes(int.Parse(minutesMatch.Groups[1].Value));

        Match secondsMatch = SecondsPattern.Match(isoDuration);
        if (secondsMatch.Success)
            total += TimeSpan.FromSeconds(int.Parse(secondsMatch.Groups[1].Value));

        Console.WriteLine($"[ParseAlexaDuration] '{isoDuration}' -> {total.TotalSeconds}s");

        return total;
    }
    #endregion

    #region ParseAlexaOrder
    // El prefijo de intent que consumen los services. ConversationIntent no lo es.
    public static string ParseAlexaOrder(IntentName alexaOrder) => alexaOrder switch
    {
        IntentName.roomba_order_ => nameof(IntentName.roomba_order_),
        IntentName.computer_status_order_ => nameof(IntentName.computer_status_order_),
        IntentName.light_order_ => nameof(IntentName.light_order_),
        _ => string.Empty
    };
    #endregion

    #region BuildAlexaResponse
    // Respuesta de una orden: habla y cierra la sesion.
    public static AlexaResponseContent BuildAlexaResponse(string intentName, string message)
    {
        string speechText = GeneralUtils.IsNullOrEmpty(message)
            ? "Comando recibido correctamente en el Home Lab."
            : message;

        return new AlexaResponseContent
        {
            OutputSpeech = new AlexaOutputSpeech { Type = "SSML", Text = speechText },
            ShouldEndSession = true,
        };
    }
    #endregion

    #region BuildConversationResponse
    // Respuesta de conversacion: si la sesion sigue abierta, lleva reprompt. Sin el,
    // alexa cierra el microfono aunque ShouldEndSession sea false.
    public static AlexaResponseContent BuildConversationResponse(string speechText, bool keepSessionOpen)
    {
        string speech = GeneralUtils.IsNullOrEmpty(speechText) ? "Si?" : speechText;

        AlexaResponseContent alexaResponseContent = new()
        {
            OutputSpeech = new AlexaOutputSpeech { Type = "PlainText", Text = speech },
            ShouldEndSession = !keepSessionOpen,
        };

        if (keepSessionOpen)
            alexaResponseContent.Reprompt = new AlexaReprompt
            {
                OutputSpeech = new AlexaOutputSpeech
                {
                    Type = "PlainText",
                    Text = "Sigues ahi? Puedes preguntarme otra cosa o decir para, para terminar.",
                }
            };

        return alexaResponseContent;
    }
    #endregion

    #region CheckSkillOrigin
    public static bool CheckSkillOrigin(AlexaSession? session, string skillId)
    {
        if (GeneralUtils.IsNullOrEmpty(skillId) || session is null)
            return false;

        return session.Application.ApplicationId == skillId;
    }
    #endregion

    #region ReadAlexaRequestJson
    // El cuerpo no se declara como parametro del endpoint porque el verificador de
    // firma necesita leerlo en crudo antes; aqui se relee del buffer, asi que el
    // pipeline tiene que haber llamado a EnableBuffering().
    public static async Task<AlexaRequestJson?> ReadAlexaRequestJson(HttpRequest request)
    {
        try
        {
            if (request.Body.CanSeek)
                request.Body.Position = 0;

            return await JsonSerializer.DeserializeAsync<AlexaRequestJson>(request.Body);
        }
        catch
        {
            return null;
        }
    }
    #endregion
}
