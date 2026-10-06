namespace WeatherApp;

/// <summary>Погодные данные по одному городу.</summary>
public sealed record WeatherData(string City, int TemperatureC, string Country)
{
    public override string ToString() => $"{City}, {Country} {Format.Temp(TemperatureC)}";
}

public static class Format
{
    /// <summary>Температура со знаком: +18 °C, -3 °C, 0 °C.</summary>
    public static string Temp(double value)
    {
        var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        var sign = rounded > 0 ? "+" : "";
        return $"{sign}{rounded} °C";
    }
}
