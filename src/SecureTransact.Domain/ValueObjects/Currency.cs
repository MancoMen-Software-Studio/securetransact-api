using System;
using System.Collections.Generic;

namespace SecureTransact.Domain.ValueObjects;

/// <summary>
/// Represents a currency with its ISO 4217 code and properties.
/// </summary>
public sealed record Currency
{
    private static readonly Dictionary<string, Currency> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = new("USD", "US Dollar", "$", 2),
        ["EUR"] = new("EUR", "Euro", "€", 2),
        ["GBP"] = new("GBP", "British Pound", "£", 2),
        ["JPY"] = new("JPY", "Japanese Yen", "¥", 0),
        ["CHF"] = new("CHF", "Swiss Franc", "CHF", 2),
        ["CAD"] = new("CAD", "Canadian Dollar", "CA$", 2),
        ["AUD"] = new("AUD", "Australian Dollar", "A$", 2),
        ["CNY"] = new("CNY", "Chinese Yuan", "¥", 2),
        ["MXN"] = new("MXN", "Mexican Peso", "$", 2),
        ["COP"] = new("COP", "Colombian Peso", "$", 2),
        ["BRL"] = new("BRL", "Brazilian Real", "R$", 2),
    };

    /// <summary>
    /// US Dollar.
    /// </summary>
    public static Currency USD => SupportedCurrencies["USD"];

    /// <summary>
    /// Euro.
    /// </summary>
    public static Currency EUR => SupportedCurrencies["EUR"];

    /// <summary>
    /// British Pound.
    /// </summary>
    public static Currency GBP => SupportedCurrencies["GBP"];

    /// <summary>
    /// Japanese Yen.
    /// </summary>
    public static Currency JPY => SupportedCurrencies["JPY"];

    /// <summary>
    /// Gets the ISO 4217 currency code (e.g., "USD", "EUR").
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the full name of the currency.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the currency symbol (e.g., "$", "€").
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the number of decimal places for this currency.
    /// </summary>
    public int DecimalPlaces { get; }

    private Currency(string code, string name, string symbol, int decimalPlaces)
    {
        Code = code;
        Name = name;
        Symbol = symbol;
        DecimalPlaces = decimalPlaces;
    }

    /// <summary>
    /// Attempts to get a currency by its ISO 4217 code.
    /// </summary>
    /// <param name="code">The ISO 4217 currency code.</param>
    /// <returns>The currency if found, null otherwise.</returns>
    public static Currency? FromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return SupportedCurrencies.GetValueOrDefault(code);
    }

    /// <summary>
    /// Checks if a currency code is supported.
    /// </summary>
    /// <param name="code">The currency code to check.</param>
    /// <returns>True if the currency is supported, false otherwise.</returns>
    public static bool IsSupported(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return SupportedCurrencies.ContainsKey(code);
    }

    /// <summary>
    /// Gets all supported currencies.
    /// </summary>
    public static IEnumerable<Currency> GetAll() => SupportedCurrencies.Values;

    /// <summary>
    /// Returns the ISO 4217 code of the currency.
    /// </summary>
    public override string ToString() => Code;
}
