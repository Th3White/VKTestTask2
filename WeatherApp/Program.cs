using System.Text;
using System.Text.Json;
using WeatherApp;

Console.OutputEncoding = Encoding.UTF8;

// 1. Загружаем список городов из файла в память.
var path = args.Length > 0 ? args[0] : FindCitiesFile();
var lines = await File.ReadAllLinesAsync(path);

// Оставляем только уникальные города (без учёта регистра и пробелов по краям).
var cities = lines
    .Select(l => l.Trim())
    .Where(l => l.Length > 0)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();

// 2. Для каждого уникального города получаем данные через API wttr.in.
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("curl/8.0");

var weather = new List<WeatherData>();
foreach (var city in cities)
{
    try
    {
        weather.Add(await FetchWeatherAsync(http, city));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Не удалось получить данные для {city}: {ex.Message}");
    }
}

// 4. Выводим информацию по каждому городу.
Console.WriteLine("Погода по городам:");
foreach (var w in weather)
    Console.WriteLine(w);

// 5. Группируем по странам: количество городов, средняя, минимальная и максимальная температура.
Console.WriteLine();
Console.WriteLine("Статистика по странам:");
var byCountry = weather
    .GroupBy(w => w.Country)
    .OrderBy(g => g.Key);

foreach (var g in byCountry)
{
    var count = g.Count();
    var avg = g.Average(w => w.TemperatureC);
    var min = g.Min(w => w.TemperatureC);
    var max = g.Max(w => w.TemperatureC);
    var word = count == 1 ? "city" : "cities";
    Console.WriteLine($"{g.Key} — {count} {word}, avg: {Format.Temp(avg)}, min: {Format.Temp(min)}, max: {Format.Temp(max)}");
}

static async Task<WeatherData> FetchWeatherAsync(HttpClient http, string city)
{
    var url = $"https://wttr.in/{Uri.EscapeDataString(city)}?format=j1";

    const int attempts = 3;
    for (var i = 1; ; i++)
    {
        try
        {
            await using var stream = await http.GetStreamAsync(url);
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            var tempC = int.Parse(root
                .GetProperty("current_condition")[0]
                .GetProperty("temp_C")
                .GetString()!);

            var country = root
                .GetProperty("nearest_area")[0]
                .GetProperty("country")[0]
                .GetProperty("value")
                .GetString()!;

            // 3. Создаём модель WeatherData.
            return new WeatherData(city, tempC, country);
        }
        catch (Exception) when (i < attempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(2 * i));
        }
    }
}

static string FindCitiesFile()
{
    // Ищем Cities.txt в текущей папке и выше (удобно при запуске через dotnet run).
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, "Cities.txt");
        if (File.Exists(candidate))
            return candidate;
        dir = dir.Parent;
    }
    throw new FileNotFoundException("Файл Cities.txt не найден. Укажите путь первым аргументом.");
}
