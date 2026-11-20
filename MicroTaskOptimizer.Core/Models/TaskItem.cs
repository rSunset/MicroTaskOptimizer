namespace MicroTaskOptimizer.Core.Models;

public class TaskItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int Priority { get; set; }
    public EnergyLevel RequiredEnergy { get; set; }
    public DateTimeOffset? Deadline { get; set; }
}