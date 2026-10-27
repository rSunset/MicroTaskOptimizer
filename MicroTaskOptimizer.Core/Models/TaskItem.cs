namespace MicroTaskOptimizer.Core.Models;

public class TaskItem
{
    public string Name { get; set; } = String.Empty;
    public int DurationMinutes { get; set; }
    public int Priority { get; set; }
    public EnergyLevel RequiredEnergy { get; set; }
}