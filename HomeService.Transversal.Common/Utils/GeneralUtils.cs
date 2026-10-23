using System.Globalization;
using System.Text;

namespace HomeService.Transversal.Common.Utils;

public static class GeneralUtils
{
    #region IsNullOrEmpty
    public static bool IsNullOrEmpty(string? value) => string.IsNullOrEmpty(value);
    #endregion

    #region IsNullOrWhiteSpace
    public static bool IsNullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);
    #endregion

    #region RemoveAccents
    // Quita tildes y dieresis para comparar lo que manda Alexa: no es consistente con
    // los acentos en los valores de slot, asi que "salon" y "salon" con tilde tienen
    // que caer en la misma rama del switch.
    public static string RemoveAccents(string? value)
    {
        if (IsNullOrEmpty(value))
            return string.Empty;

        string decomposed = value!.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(decomposed.Length);

        foreach (char character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);

        return builder.ToString();
    }
    #endregion

    #region UtcNow
    // Kind Unspecified a proposito, igual que el .replace(tzinfo = None) de python:
    // las columnas son timestamp without time zone y Npgsql rechaza escribir en
    // ellas un DateTime con Kind Utc.
    public static DateTime UtcNow() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    #endregion

    #region ParseEnum
    public static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct, System.Enum
        => System.Enum.TryParse(value?.Trim(), ignoreCase: true, out TEnum parsed) ? parsed : defaultValue;
    #endregion

    #region ParseEnumExact
    // Enum.TryParse SIN ignoreCase: hace falta para los enums cuyos nombres vienen de
    // fuera tal cual (IntentName, AlexaRequestType), donde "ConversationIntent" tiene
    // que coincidir letra a letra.
    public static TEnum? ParseEnumExact<TEnum>(string? value) where TEnum : struct, System.Enum
        => System.Enum.TryParse(value, ignoreCase: false, out TEnum parsed) ? parsed : null;
    #endregion
}
