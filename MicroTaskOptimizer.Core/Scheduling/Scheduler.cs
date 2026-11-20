using MicroTaskOptimizer.Core.Models;

namespace MicroTaskOptimizer.Core.Scheduling;

public static class Scheduler
{
    private sealed record Candidate(TaskItem Task, int Minutes, int DeadlineOffset, int Value);

    public static ScheduleResponse Build(ScheduleRequest request)
    {
        var response = new ScheduleResponse();
        var capacity = Math.Max(0, (int)Math.Floor((request.DayEnd - request.Now).TotalMinutes));
        var candidates = new List<Candidate>();

        // 1. Descartar lo que no puede entrar ni solo.
        foreach (var task in request.Tasks)
        {
            var deadlineOffset = capacity;
            if (task.Deadline is { } deadline)
            {
                var offset = (int)Math.Floor((deadline - request.Now).TotalMinutes);
                if (offset <= 0)
                {
                    response.Unscheduled.Add(new UnscheduledTask { TaskId = task.Id, Reason = ReasonCodes.DeadlinePassed });
                    continue;
                }
                deadlineOffset = Math.Min(capacity, offset);
            }

            if (task.DurationMinutes > deadlineOffset)
            {
                var reason = task.DurationMinutes > capacity ? ReasonCodes.DoesNotFit : ReasonCodes.WouldMissDeadline;
                response.Unscheduled.Add(new UnscheduledTask { TaskId = task.Id, Reason = reason });
                continue;
            }

            candidates.Add(new Candidate(task, task.DurationMinutes, deadlineOffset,
                TaskScorer.Score(task, request.Now, request.CurrentEnergy)));
        }

        // 2. Orden por deadline (a igualdad, mayor valor primero).
        candidates = candidates.OrderBy(c => c.DeadlineOffset).ThenByDescending(c => c.Value).ToList();
        var n = candidates.Count;

        // 3. Mochila 0/1: dp[t] = valor máximo con tiempo total exactamente t (-1 = inalcanzable).
        var dp = new int[capacity + 1];
        Array.Fill(dp, -1);
        dp[0] = 0;
        var taken = new bool[n, capacity + 1];

        for (var i = 0; i < n; i++)
        {
            var c = candidates[i];
            for (var t = capacity; t >= c.Minutes; t--)
            {
                // La tarea termina en el minuto t: debe respetar su deadline.
                if (t > c.DeadlineOffset || dp[t - c.Minutes] < 0) continue;
                if (dp[t - c.Minutes] + c.Value > dp[t])
                {
                    dp[t] = dp[t - c.Minutes] + c.Value;
                    taken[i, t] = true;
                }
            }
        }

        // 4. Reconstruir qué tareas se eligieron.
        var bestT = 0;
        for (var t = 1; t <= capacity; t++)
            if (dp[t] > dp[bestT]) bestT = t;

        var chosen = new bool[n];
        var remaining = bestT;
        for (var i = n - 1; i >= 0; i--)
        {
            if (!taken[i, remaining]) continue;
            chosen[i] = true;
            remaining -= candidates[i].Minutes;
        }

        // 5. Asignar horas consecutivas desde 'now' y armar la respuesta.
        var cursor = 0;
        for (var i = 0; i < n; i++)
        {
            var c = candidates[i];
            if (chosen[i])
            {
                response.Schedule.Add(new ScheduledBlock
                {
                    TaskId = c.Task.Id,
                    Name = c.Task.Name,
                    Start = request.Now.AddMinutes(cursor),
                    End = request.Now.AddMinutes(cursor + c.Minutes)
                });
                cursor += c.Minutes;

                if (c.Task.RequiredEnergy > request.CurrentEnergy)
                    response.Warnings.Add(new ScheduleWarning { TaskId = c.Task.Id, Code = ReasonCodes.EnergyBelowRequired });
            }
            else
            {
                var reason = c.Task.Deadline is not null && c.DeadlineOffset < capacity
                    ? ReasonCodes.WouldMissDeadline
                    : ReasonCodes.DoesNotFit;
                response.Unscheduled.Add(new UnscheduledTask { TaskId = c.Task.Id, Reason = reason });
            }
        }

        return response;
    }
}