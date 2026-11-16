using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Text.Json;
using MicroTaskOptimizer.Core.Models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MicroTaskOptimizer.Lambda;

public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    
    private static APIGatewayProxyResponse CreateResponse(int statusCode, object body) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(body)
    };
    
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation("Petición recibida.");
        ScheduleRequest? scheduleRequest;
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return CreateResponse(400, new { error = "El cuerpo de la petición está vacío."});
        }
        
        try
        {
            scheduleRequest = JsonSerializer.Deserialize<ScheduleRequest>(request.Body, JsonOptions);
        }
        catch (JsonException e)
        {
            context.Logger.LogWarning($"JSON inválido: {e.Message}");
            return CreateResponse(400, new { error = "El JSON de la petición no es válido."});
        }
        
        if (scheduleRequest?.Tasks is null)
        {
            return CreateResponse(400, new { error = "El campo ‘tasks’ es obligatorio"});
        }
        
        context.Logger.LogInformation($"Tareas recibidas: {scheduleRequest.Tasks.Count}");
        // TODO (micro-tarea futura): filtrar tareas por energía
        // TODO (micro-tarea futura): ALGORITMO DE LA MOCHILA 0/1 AQUÍ

        return CreateResponse(200, new { recievedChars = request.Body.Length});
    }
}