namespace MicroTaskOptimizer.Core.Models;

public class ScheduleResponse
{
    public List<ScheduledBlock> Schedule { get; set; } = new();
    public List<UnscheduledTask> Unscheduled { get; set; } = new();
    public List<ScheduleWarning> Warnings { get; set; } = new();
}

public class ScheduledBlock
{
    public string TaskId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
}

public class UnscheduledTask
{
    public string TaskId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class ScheduleWarning
{
    public string TaskId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public static class ReasonCodes
{
    public const string DoesNotFit = "DoesNotFit";
    public const string WouldMissDeadline = "WouldMissDeadline";
    public const string DeadlinePassed = "DeadlinePassed";
    public const string EnergyBelowRequired = "EnergyBelowRequired";
}