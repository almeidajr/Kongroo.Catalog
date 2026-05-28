using System.Reflection;
using System.Text.Json.Serialization;
using Kongroo.Catalog.Domain;
using Shouldly;

namespace Kongroo.Catalog.UnitTests.Catalog.Domain;

public sealed class CurrencyTests
{
    [Fact]
    public void Definition_WithCurrencyEnumMembers_ShouldHaveMatchingCodeConstantNames()
    {
        // Arrange
        var enumNames = Enum.GetNames<Currency>().OrderBy(name => name);
        var codeFields = typeof(CurrencyCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
            .OrderBy(field => field.Name);

        // Act
        var codeNames = codeFields.Select(field => field.Name);

        // Assert
        enumNames.ShouldBe(codeNames);
    }

    [Fact]
    public void Definition_WithCurrencyEnumMembers_ShouldHaveMatchingJsonStringValues()
    {
        // Arrange
        var currencyFields = typeof(Currency)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .OrderBy(field => field.Name);
        var codeValues = typeof(CurrencyCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
            .OrderBy(field => field.Name)
            .Select(field => (string)field.GetRawConstantValue()!);

        // Act
        var jsonNames = currencyFields
            .Select(field => field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name)
            .ToArray();

        // Assert
        jsonNames.ShouldNotContain(name => name == null);
        jsonNames.ShouldBe(codeValues);
    }

    [Fact]
    public void ToCode_WithDefinedCurrency_ShouldRoundTripThroughFromCode()
    {
        // Arrange
        var currencies = Enum.GetValues<Currency>();

        // Act
        var codes = currencies.Select(CurrencyMappings.ToCode).ToArray();
        var roundTripCurrencies = codes.Select(CurrencyMappings.FromCode);

        // Assert
        codes.Distinct().Count().ShouldBe(currencies.Length);
        roundTripCurrencies.ShouldBe(currencies);
    }

    [Fact]
    public void FromCode_WithDefinedCode_ShouldRoundTripThroughToCode()
    {
        // Arrange
        var codes = typeof(CurrencyCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        // Act
        var currencies = codes.Select(CurrencyMappings.FromCode).ToArray();
        var roundTripCodes = currencies.Select(CurrencyMappings.ToCode);

        // Assert
        currencies.Distinct().Count().ShouldBe(codes.Length);
        roundTripCodes.ShouldBe(codes);
    }
}

