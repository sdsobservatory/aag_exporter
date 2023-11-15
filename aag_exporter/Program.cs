using Prometheus;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

const string JsonFile = "aag_json.dat";
const string DebugFile = "DebugData.txt";

Gauge AagCloudsSafe = Metrics.CreateGauge("aag_clouds_safe", "1 when clouds are safe.");
Gauge AagRainSafe = Metrics.CreateGauge("aag_rain_safe", "1 when rain is safe.");
Gauge AagLightSafe = Metrics.CreateGauge("aag_light_safe", "1 when light is safe.");
Gauge AagHumiditySafe = Metrics.CreateGauge("aag_humidity_safe", "1 when humidity is safe.");
Gauge AagPressureSafe = Metrics.CreateGauge("aag_pressure_safe", "1 when pressure is safe.");
Gauge AagSafe = Metrics.CreateGauge("aag_safe", "1 when all sensors are safe.");
Gauge AagSwitch = Metrics.CreateGauge("aag_switch", "1 when the relay is closed.");
Gauge AagCloudTemperature = Metrics.CreateGauge("aag_cloud_temperature", "Cloud temperature in degrees C.");
Gauge AagTemperature = Metrics.CreateGauge("aag_temperature", "Air temperature in degrees C.");
Gauge AagWind = Metrics.CreateGauge("aag_wind", "Wind speed in km/h.");
Gauge AagGust = Metrics.CreateGauge("aag_gust", "Wind gust speed in km/h.");
Gauge AagRain = Metrics.CreateGauge("aag_rain", "Rain in arbitrary units.");
Gauge AagLight = Metrics.CreateGauge("aag_light", "Light in arbitrary units.");
Gauge AagHumidity = Metrics.CreateGauge("aag_humidity", "Relative humidity 0 to 100 percent.");
Gauge AagDewPoint = Metrics.CreateGauge("aag_dewpoint", "Dew point in degrees C.");
Gauge AagAbsolutePressure = Metrics.CreateGauge("aag_abs_pressure", "Absolute pressure in mbar.");
Gauge AagRelativePressure = Metrics.CreateGauge("aag_rel_pressure", "Relative pressure in mbar.");
Gauge AagRawInfrared = Metrics.CreateGauge("aag_rawir", "Raw infrared in arbitrary units.");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMetricServer(options =>
{
    options.Port = 9100;
});

var aagDirectory = builder.Configuration.GetValue<string>("AagDirectory");
if (string.IsNullOrEmpty(aagDirectory) || !Directory.Exists(aagDirectory))
    throw new DirectoryNotFoundException("AAG Directory not found or configured");

var app = builder.Build();

Metrics.SuppressDefaultMetrics();
Metrics.DefaultRegistry.AddBeforeCollectCallback(cancel =>
{
    var aagData = GetAagData();
    AagCloudsSafe.Set(aagData.CloudsSafe ? 1 : 0);
    AagRainSafe.Set(aagData.RainSafe ? 1 : 0);
    AagLightSafe.Set(aagData.LightSafe ? 1 : 0);
    AagHumiditySafe.Set(aagData.HumiditySafe ? 1 : 0);
    AagPressureSafe.Set(aagData.PressureSafe ? 1 : 0);
    AagSafe.Set(aagData.Safe ? 1 : 0);
    AagSwitch.Set(aagData.Switch ? 1 : 0);
    AagCloudTemperature.Set(aagData.Clouds);
    AagGust.Set(aagData.Gust);
    AagRain.Set(aagData.Rain);
    AagLight.Set(aagData.Light);
    AagAbsolutePressure.Set(aagData.AbsolutePressure);
    AagRelativePressure.Set(aagData.RelativePressure);
    AagRawInfrared.Set(aagData.RawInfrared);
    AagTemperature.Set(aagData.Temperature);
    AagWind.Set(aagData.Wind);
    AagHumidity.Set(aagData.Humidity);
    AagDewPoint.Set(aagData.DewPoint);
    return Task.CompletedTask;
});

app.MapGet("/aag", () =>
{
    var data = GetAagData();
    return new AagData
    {
        Timestamp = data.DateLocalTime.ToUniversalTime(),
        CwInfo = data.CwInfo,
        SldData = data.SldData,
        Clouds = data.Clouds,
        CloudsSafe = data.CloudsSafe,
        Temperature = data.Temperature,
        Wind = data.Wind,
        WindSafe = data.WindSafe,
        Gust = data.Gust,
        Rain = data.Rain,
        RainSafe = data.RainSafe,
        Light = data.Light,
        LightSafe = data.LightSafe,
        Switch = data.Switch,
        Safe = data.Safe,
        Humidity = data.Humidity,
        HumiditySafe = data.HumiditySafe,
        DewPoint = data.DewPoint,
        AbsolutePressure = data.AbsolutePressure,
        RelativePressure = data.RelativePressure,
        PressureSafe = data.PressureSafe,
        RawInfrared = data.RawInfrared,
    };
});
app.MapGet("/debug", () =>
{
    var debugText = File.ReadAllText(Path.Combine(aagDirectory, DebugFile), Encoding.UTF8);
    return debugText.ReplaceLineEndings("\n");
});
app.Run();

AagRaw GetAagData()
{
    var jsonText = File.ReadAllText(Path.Combine(aagDirectory, JsonFile), Encoding.UTF8);
    var aagData = JsonSerializer.Deserialize<AagRaw>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new NullReferenceException();
    return aagData;
}

internal class AagDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(DateTime));
        var value = DateTime.ParseExact(reader.GetString()!, "yyyy/MM/dd HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeLocal);
        return value;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime());
    }
}

internal class SafeToBooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()!.Equals("safe", StringComparison.OrdinalIgnoreCase);
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}

internal class IntegerToBooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetInt32() switch
        {
            1 => true,
            _ => false,
        };
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}

internal record AagRaw
{
    [JsonConverter(typeof(AagDateTimeConverter))]
    public DateTime DateLocalTime { get; set; }

    public string CwInfo { get; init; } = string.Empty;

    public string SldData { get; init; } = string.Empty;

    public double Clouds { get; init; }

    [JsonConverter(typeof(SafeToBooleanConverter))]
    public bool CloudsSafe { get; init; }

    [JsonPropertyName("temp")]
    public double Temperature { get; init; }
    
    public double Wind { get; init; }

    [JsonConverter(typeof(SafeToBooleanConverter))]
    public bool WindSafe { get; init; }

    public double Gust { get;init; }
    
    public int Rain { get; init; }

    [JsonConverter(typeof(SafeToBooleanConverter))]
    public bool RainSafe { get; init; }
    
    public int Light { get; init; }

    [JsonConverter(typeof(SafeToBooleanConverter))]
    public bool LightSafe { get; init; }
    
    [JsonConverter(typeof(IntegerToBooleanConverter))]
    public bool Switch { get; init; }

    [JsonConverter(typeof(IntegerToBooleanConverter))]
    public bool Safe { get; init; }

    [JsonPropertyName("hum")]
    public double Humidity { get; init; }

    [JsonPropertyName("humSafe")]
    [JsonConverter(typeof(SafeToBooleanConverter))]
    public bool HumiditySafe { get; init; }

    [JsonPropertyName("dewp")]
    public double DewPoint { get; init; }

    [JsonPropertyName("abspress")]
    public double AbsolutePressure { get; init; }

    [JsonPropertyName("relpress")]
    public double RelativePressure { get; init; }

    [JsonConverter(typeof(SafeToBooleanConverter))]
    public bool PressureSafe { get; init; }

    [JsonPropertyName("rawir")]
    public double RawInfrared { get; init; }
}

internal record AagData
{
    public required DateTime Timestamp { get; init; }
    public required string CwInfo { get; init; }
    public required string SldData { get; init; }
    public required double Clouds { get; init; }
    public required bool CloudsSafe { get; init; }
    public required double Temperature { get; init; }
    public required double Wind { get; init; }
    public required bool WindSafe { get; init; }
    public required double Gust { get; init; }
    public required int Rain { get; init; }
    public required bool RainSafe { get; init; }
    public required int Light { get; init; }
    public required bool LightSafe { get; init; }
    public required bool Switch { get; init; }
    public required bool Safe { get; init; }
    public required double Humidity { get; init; }
    public required bool HumiditySafe { get; init; }
    public required double DewPoint { get; init; }
    public required double AbsolutePressure { get; init; }
    public required double RelativePressure { get; init; }
    public required bool PressureSafe { get; init; }
    public required double RawInfrared { get; init; }
}