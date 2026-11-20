using MicroTaskOptimizer.Core.Models;
using MicroTaskOptimizer.Core.Scheduling;

namespace MicroTaskOptimizer.Core.Tests;

public class SchedulerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 9, 0, 0, TimeSpan.FromHours(-6));

    private static TaskItem MakeTask(string id, int minutes, int priority,
        EnergyLevel energy = EnergyLevel.Low, int? deadlineInMinutes = null) => new()
    {
        Id = id, Name = id, DurationMinutes = minutes, Priority = priority, RequiredEnergy = energy,
        Deadline = deadlineInMinutes is null ? null : Now.AddMinutes(deadlineInMinutes.Value)
    };

    [Fact]
    public void Build_ExampleFromDesignDoc_SchedulesBothInDeadlineOrderWithEnergyWarning()
    {
        var request = new ScheduleRequest
        {
            Now = Now, DayEnd = Now.AddMinutes(690), CurrentEnergy = EnergyLevel.Medium,
            Tasks = { MakeTask("proyecto", 120, 5, EnergyLevel.High, 480), MakeTask("platos", 15, 2) }
        };

        var response = Scheduler.Build(request);

        Assert.Equal(2, response.Schedule.Count);
        Assert.Equal("proyecto", response.Schedule[0].TaskId);
        Assert.Equal(Now, response.Schedule[0].Start);
        Assert.Equal(Now.AddMinutes(120), response.Schedule[1].Start);
        Assert.Empty(response.Unscheduled);
        Assert.Equal("proyecto", Assert.Single(response.Warnings).TaskId);
    }

    [Fact]
    public void Build_TwoTasksSameDeadlineOnlyOneFits_KeepsHigherValue()
    {
        var request = new ScheduleRequest
        {
            Now = Now, DayEnd = Now.AddMinutes(180), CurrentEnergy = EnergyLevel.Low,
            Tasks = { MakeTask("A", 90, 3, deadlineInMinutes: 100), MakeTask("B", 90, 5, deadlineInMinutes: 100) }
        };

        var response = Scheduler.Build(request);

        Assert.Equal("B", Assert.Single(response.Schedule).TaskId);
        var left = Assert.Single(response.Unscheduled);
        Assert.Equal("A", left.TaskId);
        Assert.Equal(ReasonCodes.WouldMissDeadline, left.Reason);
    }

    [Fact]
    public void Build_DeadlineAlreadyPassed_GoesToUnscheduled()
    {
        var request = new ScheduleRequest
        {
            Now = Now, DayEnd = Now.AddMinutes(180), CurrentEnergy = EnergyLevel.Low,
            Tasks = { MakeTask("vencida", 30, 5, deadlineInMinutes: -10) }
        };

        var response = Scheduler.Build(request);

        Assert.Empty(response.Schedule);
        Assert.Equal(ReasonCodes.DeadlinePassed, Assert.Single(response.Unscheduled).Reason);
    }
}