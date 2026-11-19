using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MicroTaskOptimizer.Core.Models;
using MicroTaskOptimizer.Core.Scheduling;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MicroTaskOptimizer.Lambda;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static APIGatewayProxyResponse CreateResponse(int statusCode, object body) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(body, JsonOptions)
    };

    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation("Petición recibida.");

        if (string.IsNullOrWhiteSpace(request.Body))
            return CreateResponse(400, new { errors = new[] { "El cuerpo de la petición está vacío." } });

        ScheduleRequest? scheduleRequest;
        try
        {
            scheduleRequest = JsonSerializer.Deserialize<ScheduleRequest>(request.Body, JsonOptions);
        }
        catch (JsonException e)
        {
            context.Logger.LogWarning($"JSON inválido: {e.Message}");
            return CreateResponse(400, new { errors = new[] { "El JSON de la petición no es válido." } });
        }

        if (scheduleRequest?.Tasks is null)
            return CreateResponse(400, new { errors = new[] { "El campo 'tasks' es obligatorio." } });

        var errors = ScheduleRequestValidator.Validate(scheduleRequest);
        if (errors.Count > 0)
        {
            context.Logger.LogWarning($"Petición inválida: {string.Join(" | ", errors)}");
            return CreateResponse(400, new { errors });
        }

        var response = Scheduler.Build(scheduleRequest);
        context.Logger.LogInformation($"Programadas: {response.Schedule.Count}, fuera: {response.Unscheduled.Count}");
        return CreateResponse(200, response);
    }
}