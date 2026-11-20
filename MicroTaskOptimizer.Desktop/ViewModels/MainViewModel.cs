using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicroTaskOptimizer.Core.Models;
using MicroTaskOptimizer.Desktop.Services;

namespace MicroTaskOptimizer.Desktop.ViewModels;

public record EnergyOption(EnergyLevel Value, string Label);

public record BlockView(string TaskId, string Time, string Name);

public record TaskRow(TaskItem Task)
{
    public string Name => Task.Name;
    public string Details =>
        $"{Task.DurationMinutes} min · Prioridad {Task.Priority} · Energía {MainViewModel.LabelOf(Task.RequiredEnergy)}"
        + (Task.Deadline is { } d ? $" · Vence {d:dd/MM HH:mm}" : "");
}

public partial class MainViewModel : ViewModelBase
{
    public static readonly EnergyOption[] EnergyChoices =
    {
        new(EnergyLevel.Low, "Baja"),
        new(EnergyLevel.LowMedium, "Media-baja"),
        new(EnergyLevel.Medium, "Media"),
        new(EnergyLevel.MediumHigh, "Media-alta"),
        new(EnergyLevel.High, "Alta")
    };

    public static string LabelOf(EnergyLevel level) => EnergyChoices.First(o => o.Value == level).Label;

    private readonly ScheduleApiClient _api = new();

    public EnergyOption[] EnergyOptions => EnergyChoices;
    public DateTime Today { get; } = DateTime.Today;

    public ObservableCollection<TaskRow> Tasks { get; } = new();
    public ObservableCollection<BlockView> Schedule { get; } = new();
    public ObservableCollection<string> Unscheduled { get; } = new();
    public ObservableCollection<string> Warnings { get; } = new();

    public bool HasTasks => Tasks.Count > 0;
    public bool HasSchedule => Schedule.Count > 0;
    public bool HasUnscheduled => Unscheduled.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;

    [ObservableProperty] private EnergyOption _currentEnergy = EnergyChoices[2];
    [ObservableProperty] private TimeSpan? _dayEndTime = new TimeSpan(21, 0, 0);
    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private decimal? _newDuration = 30;
    [ObservableProperty] private decimal? _newPriority = 3;
    [ObservableProperty] private EnergyOption _newEnergy = EnergyChoices[2];
    [ObservableProperty] private DateTime? _newDeadlineDate;
    [ObservableProperty] private TimeSpan? _newDeadlineTime;
    [ObservableProperty] private TaskRow? _selectedTask;
    [ObservableProperty] private BlockView? _selectedBlock;
    [ObservableProperty] private string _status = "";

    public MainViewModel()
    {
        Watch(Tasks, nameof(HasTasks));
        Watch(Schedule, nameof(HasSchedule));
        Watch(Unscheduled, nameof(HasUnscheduled));
        Watch(Warnings, nameof(HasWarnings));
    }

    private void Watch(INotifyCollectionChanged collection, string property) =>
        collection.CollectionChanged += (_, _) => OnPropertyChanged(property);

    // Si la energía cambia y ya hay un horario, se recalcula solo.
    partial void OnCurrentEnergyChanged(EnergyOption value)
    {
        if (HasTasks && HasSchedule) _ = GenerateCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewName)) { Status = "Escribe qué tienes que hacer."; return; }

        var minutes = (int)(NewDuration ?? 0);
        if (minutes <= 0) { Status = "La duración debe ser mayor que 0 minutos."; return; }

        var priority = (int)(NewPriority ?? 0);
        if (priority is < 1 or > 5) { Status = "La prioridad debe estar entre 1 y 5."; return; }

        DateTimeOffset? deadline = null;
        if (NewDeadlineDate is not null || NewDeadlineTime is not null)
        {
            var day = NewDeadlineDate ?? DateTime.Today;                    // sin fecha = hoy
            var time = NewDeadlineTime ?? new TimeSpan(23, 59, 0);          // sin hora = 23:59
            deadline = new DateTimeOffset(day.Date + time);
            if (deadline.Value <= DateTimeOffset.Now)
            {
                Status = "Esa fecha límite ya pasó. Elige un momento posterior al actual.";
                return;
            }
        }

        Tasks.Add(new TaskRow(new TaskItem
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = NewName.Trim(),
            DurationMinutes = minutes,
            Priority = priority,
            RequiredEnergy = NewEnergy.Value,
            Deadline = deadline
        }));

        NewName = "";
        ClearDeadline();
        Status = "Tarea agregada.";
    }

    [RelayCommand]
    private void ClearDeadline()
    {
        NewDeadlineDate = null;
        NewDeadlineTime = null;
    }

    [RelayCommand]
    private void RemoveTask()
    {
        if (SelectedTask is not null) Tasks.Remove(SelectedTask);
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        Schedule.Clear(); Unscheduled.Clear(); Warnings.Clear();

        if (!HasTasks) { Status = "Agrega al menos una tarea."; return; }
        if (DayEndTime is null) { Status = "Indica a qué hora termina tu día."; return; }

        var dayEnd = new DateTimeOffset(DateTime.Today + DayEndTime.Value);
        if (dayEnd <= DateTimeOffset.Now)
        {
            Status = "La hora de fin del día debe ser posterior a la hora actual.";
            return;
        }

        var request = new ScheduleRequest
        {
            Now = DateTimeOffset.Now,
            DayEnd = dayEnd,
            CurrentEnergy = CurrentEnergy.Value,
            Tasks = Tasks.Select(r => r.Task).ToList()
        };

        try
        {
            Status = "Calculando...";
            var response = await _api.GetScheduleAsync(request);
            ShowResult(response);
            Status = $"Horario generado a las {DateTime.Now:HH:mm}.";
        }
        catch (Exception e)
        {
            Status = e.Message;
        }
    }

    // "Hecha" saca la tarea de la lista y recalcula con la hora actual.
    [RelayCommand]
    private async Task MarkDoneAsync()
    {
        if (SelectedBlock is null) { Status = "Selecciona primero un bloque del horario."; return; }

        var row = Tasks.FirstOrDefault(r => r.Task.Id == SelectedBlock.TaskId);
        if (row is not null) Tasks.Remove(row);
        await GenerateAsync();
    }

    private void ShowResult(ScheduleResponse response)
    {
        var names = Tasks.ToDictionary(r => r.Task.Id, r => r.Task.Name);
        string NameOf(string id) => names.GetValueOrDefault(id, id);

        foreach (var b in response.Schedule)
            Schedule.Add(new BlockView(b.TaskId, $"{b.Start:HH:mm} – {b.End:HH:mm}", b.Name));

        foreach (var u in response.Unscheduled)
            Unscheduled.Add($"{NameOf(u.TaskId)}: {Describe(u.Reason)}");

        foreach (var w in response.Warnings)
            Warnings.Add($"{NameOf(w.TaskId)}: exige más energía de la que tienes ahora.");
    }

    private static string Describe(string code) => code switch
    {
        "DoesNotFit" => "no cabe en el tiempo disponible",
        "WouldMissDeadline" => "no alcanzaría a terminar antes de su fecha límite",
        "DeadlinePassed" => "su fecha límite ya pasó",
        _ => code
    };
}