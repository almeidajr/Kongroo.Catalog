using System.Text.Json.Serialization;

namespace Kongroo.Catalog.Domain;

public enum Currency
{
    [JsonStringEnumMemberName(CurrencyCodes.Brl)]
    Brl,

    [JsonStringEnumMemberName(CurrencyCodes.Eur)]
    Eur,

    [JsonStringEnumMemberName(CurrencyCodes.Usd)]
    Usd,
}

public static class CurrencyCodes
{
    public const string Brl = "BRL";
    public const string Eur = "EUR";
    public const string Usd = "USD";
}

public static class CurrencyMappings
{
    public const int Length = 3;

    public static string ToCode(Currency currency) =>
        currency switch
        {
            Currency.Brl => CurrencyCodes.Brl,
            Currency.Eur => CurrencyCodes.Eur,
            Currency.Usd => CurrencyCodes.Usd,
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unsupported currency."),
        };

    public static Currency FromCode(string code) =>
        code switch
        {
            CurrencyCodes.Brl => Currency.Brl,
            CurrencyCodes.Eur => Currency.Eur,
            CurrencyCodes.Usd => Currency.Usd,
            _ => throw new ArgumentException($"Unsupported currency: {code}", nameof(code)),
        };
}
