using MicroTaskOptimizer.Core.Models;

namespace MicroTaskOptimizer.Core.Scheduling;

public static class TaskScorer

{
    public static int Score(TaskItem task, DateTimeOffset now, EnergyLevel currentEnergy)
    {
        var urgency = 1.0;
        if (task.Deadline is { } deadline)
        {
            var slack = (deadline - now).TotalMinutes - task.DurationMinutes;
            urgency = slack <= 60 ? 2.0 : slack <= 240 ? 1.5 : 1.0;
        }

        var gap = Math.Max(0, (int)task.RequiredEnergy - (int)currentEnergy);
        var energy = Math.Max(0.4, 1 - 0.15 * gap);
        return (int)Math.Round(task.Priority * 10 * urgency * energy);
    }
}