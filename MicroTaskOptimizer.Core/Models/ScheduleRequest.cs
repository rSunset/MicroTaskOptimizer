namespace MicroTaskOptimizer.Core.Models;

public class ScheduleRequest
{
    public DateTimeOffset Now { get; set; }
    public DateTimeOffset DayEnd { get; set; }
    public EnergyLevel CurrentEnergy { get; set; }
    public List<TaskItem> Tasks { get; set; } = new();
}