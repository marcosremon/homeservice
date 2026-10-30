using HomeService.Domain.Model.Enum.Alexa;
using HomeService.Domain.Model.Enum.Roomba;
using HomeService.Transversal.Common.Utils;

namespace HomeService.Service.UnitTests.Utils
{
    public class GeneralUtilsTests
    {
        #region IsNullOrEmpty
        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData(" ", false)]
        [InlineData("a", false)]
        public void IsNullOrEmpty(string? value, bool expected)
        {
            Assert.Equal(expected, GeneralUtils.IsNullOrEmpty(value));
        }
        #endregion

        #region IsNullOrWhiteSpace
        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("  \t", true)]
        [InlineData(" a ", false)]
        public void IsNullOrWhiteSpace(string? value, bool expected)
        {
            Assert.Equal(expected, GeneralUtils.IsNullOrWhiteSpace(value));
        }
        #endregion

        #region RemoveAccents
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("salón", "salon")]
        [InlineData("Habitación de Diego", "Habitacion de Diego")]
        [InlineData("pingüino", "pinguino")]
        [InlineData("baño", "bano")]
        [InlineData("ÁÉÍÓÚ", "AEIOU")]
        [InlineData("sin tildes", "sin tildes")]
        public void RemoveAccents(string? value, string expected)
        {
            Assert.Equal(expected, GeneralUtils.RemoveAccents(value));
        }
        #endregion

        #region UtcNow_HasUnspecifiedKindAndUtcValue
        // Npgsql rechaza un DateTime Utc en columnas "timestamp without time zone".
        [Fact]
        public void UtcNow_HasUnspecifiedKindAndUtcValue()
        {
            DateTime before = DateTime.UtcNow;
            DateTime now = GeneralUtils.UtcNow();
            DateTime after = DateTime.UtcNow;

            Assert.Equal(DateTimeKind.Unspecified, now.Kind);
            Assert.InRange(now.Ticks, before.Ticks, after.Ticks);
        }
        #endregion

        #region ParseEnum_KnownValues_IgnoreCaseAndSpaces
        [Theory]
        [InlineData("KITCHEN", RoombaTarget.KITCHEN)]
        [InlineData("kitchen", RoombaTarget.KITCHEN)]
        [InlineData("  Bedrooms  ", RoombaTarget.BEDROOMS)]
        [InlineData("full_house", RoombaTarget.FULL_HOUSE)]
        public void ParseEnum_KnownValues_IgnoreCaseAndSpaces(string value, RoombaTarget expected)
        {
            Assert.Equal(expected, GeneralUtils.ParseEnum(value, RoombaTarget.LIVING_ROOM));
        }
        #endregion

        #region ParseEnum_UnknownValues_ReturnDefault
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("garaje")]
        [InlineData("KITCHEN AND GRANDMOTHER")]
        public void ParseEnum_UnknownValues_ReturnDefault(string? value)
        {
            Assert.Equal(RoombaPhase.STOP, GeneralUtils.ParseEnum(value, RoombaPhase.STOP));
        }
        #endregion

        #region ParseEnumExact_IsCaseSensitive
        [Theory]
        [InlineData("ConversationIntent", IntentName.ConversationIntent)]
        [InlineData("roomba_order_", IntentName.roomba_order_)]
        [InlineData("light_order_", IntentName.light_order_)]
        public void ParseEnumExact_MatchesExactName(string value, IntentName expected)
        {
            Assert.Equal(expected, GeneralUtils.ParseEnumExact<IntentName>(value));
        }
        #endregion

        #region ParseEnumExact_DifferentCaseOrUnknown_ReturnsNull
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("conversationintent")]
        [InlineData("CONVERSATIONINTENT")]
        [InlineData("Roomba_Order_")]
        [InlineData("light_order_encender_luces_salon")]
        public void ParseEnumExact_DifferentCaseOrUnknown_ReturnsNull(string? value)
        {
            Assert.Null(GeneralUtils.ParseEnumExact<IntentName>(value));
        }
        #endregion
    }
}
