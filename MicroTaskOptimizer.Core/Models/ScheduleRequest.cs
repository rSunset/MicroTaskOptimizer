namespace MicroTaskOptimizer.Core.Models;

public class ScheduleRequest
{
    public List<TaskItem> Tasks { get; set; } = new();
    public int AvailableMinutes { get; set; }
    public EnergyLevel CurrentEnergy { get; set; }
}