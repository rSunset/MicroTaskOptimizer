using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MicroTaskOptimizer.Core.Models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MicroTaskOptimizer.Lambda;

public class Function
{
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation("Petición recibida.");
        
        // TODO (micro-tarea futura): deserializar request.Body a una lista de TaskItem
        // TODO (micro-tarea futura): filtrar tareas por energía
        // TODO (micro-tarea futura): ALGORITMO DE LA MOCHILA 0/1 AQUÍ

        return new APIGatewayProxyResponse()
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = $"{{\"receivedChars\":{request.Body?.Length ?? 0}}}"
        };
    }
}