using MicroTaskOptimizer.Core.Models;

namespace MicroTaskOptimizer.Core.Scheduling;

public static class ScheduleRequestValidator
{
    public static List<string> Validate(ScheduleRequest request)
    {
        var errors = new List<string>();

        var window = (request.DayEnd - request.Now).TotalMinutes;
        if (window <= 0 || window > 1440)
            errors.Add("La ventana entre 'now' y 'dayEnd' debe ser mayor que 0 y de máximo 24 horas.");
        if (!Enum.IsDefined(request.CurrentEnergy))
            errors.Add("'currentEnergy' no es un valor válido.");

        var seenIds = new HashSet<string>();
        for (var i = 0; i < request.Tasks.Count; i++)
        {
            var task = request.Tasks[i];
            var label = $"Tarea {i + 1}";

            if (string.IsNullOrWhiteSpace(task.Id)) errors.Add($"{label}: 'id' es obligatorio.");
            else if (!seenIds.Add(task.Id)) errors.Add($"{label}: el id '{task.Id}' está duplicado.");

            if (string.IsNullOrWhiteSpace(task.Name)) errors.Add($"{label}: 'name' es obligatorio.");
            if (task.DurationMinutes <= 0) errors.Add($"{label}: la duración debe ser mayor que 0.");
            if (task.Priority is < 1 or > 5) errors.Add($"{label}: la prioridad debe estar entre 1 y 5.");
            if (!Enum.IsDefined(task.RequiredEnergy)) errors.Add($"{label}: 'requiredEnergy' no es válido.");
        }

        return errors;
    }
}