using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MicroTaskOptimizer.Core.Models;

namespace MicroTaskOptimizer.Desktop.Services;

public class ScheduleApiClient
{
    private const string ApiUrl = "https://h9rpatnx4j.execute-api.us-east-2.amazonaws.com/prod/schedule";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ScheduleResponse> GetScheduleAsync(ScheduleRequest request)
    {
        using var content = new StringContent(JsonSerializer.Serialize(request, Json), Encoding.UTF8, "application/json");
        using var httpResponse = await Http.PostAsync(ApiUrl, content);
        var text = await httpResponse.Content.ReadAsStringAsync();

        if (!httpResponse.IsSuccessStatusCode)
            throw new InvalidOperationException(ExtractErrors(text, (int)httpResponse.StatusCode));

        return JsonSerializer.Deserialize<ScheduleResponse>(text, Json) ?? new ScheduleResponse();
    }

    private static string ExtractErrors(string body, int status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errors))
                return string.Join(" ", errors.EnumerateArray().Select(e => e.GetString()));
        }
        catch (JsonException) { }
        return $"Error del servidor ({status}).";
    }
}