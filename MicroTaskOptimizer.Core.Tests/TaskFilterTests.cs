using MicroTaskOptimizer.Core.Models;
using MicroTaskOptimizer.Core.Scheduling;

namespace MicroTaskOptimizer.Core.Tests;

public class TaskFilterTests
{
    [Fact]
    public void FilterByEnergy_HighEnergy_PreserversAllTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new() { Name = "Estudiar", RequiredEnergy = EnergyLevel.High },
            new() { Name = "Elaborar repositorio", RequiredEnergy = EnergyLevel.MediumHigh},
            new() { Name = "Responder correos", RequiredEnergy = EnergyLevel.Medium },
            new() { Name = "Actualizar pendientes", RequiredEnergy = EnergyLevel.LowMedium},
            new() { Name = "Lavar platos", RequiredEnergy = EnergyLevel.Low }
        };

        // Act
        var result = TaskFilter.FilterByEnergy(tasks, EnergyLevel.High);

        // Assert
        Assert.Equal(5, result.Count);
    }
    
    [Fact]
    public void FilterByEnergy_HighMediumEnergy_DiscardsHigherEnergyTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new() { Name = "Estudiar", RequiredEnergy = EnergyLevel.High },
            new() { Name = "Elaborar repositorio", RequiredEnergy = EnergyLevel.MediumHigh},
            new() { Name = "Responder correos", RequiredEnergy = EnergyLevel.Medium },
            new() { Name = "Actualizar pendientes", RequiredEnergy = EnergyLevel.LowMedium},
            new() { Name = "Lavar platos", RequiredEnergy = EnergyLevel.Low }
        };

        // Act
        var result = TaskFilter.FilterByEnergy(tasks, EnergyLevel.MediumHigh);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.DoesNotContain(result, t => t.Name == "Estudiar");
    }
    
    [Fact]
    public void FilterByEnergy_MediumEnergy_DiscardsHigherEnergyTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new() { Name = "Estudiar", RequiredEnergy = EnergyLevel.High },
            new() { Name = "Elaborar repositorio", RequiredEnergy = EnergyLevel.MediumHigh},
            new() { Name = "Responder correos", RequiredEnergy = EnergyLevel.Medium },
            new() { Name = "Actualizar pendientes", RequiredEnergy = EnergyLevel.LowMedium},
            new() { Name = "Lavar platos", RequiredEnergy = EnergyLevel.Low }
        };

        // Act
        var result = TaskFilter.FilterByEnergy(tasks, EnergyLevel.Medium);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, t => t.Name == "Estudiar");
        Assert.DoesNotContain(result, t => t.Name == "Elaborar repositorio");
    }
    
    [Fact]
    public void FilterByEnergy_LowMediumEnergy_DiscardsHigherEnergyTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new() { Name = "Estudiar", RequiredEnergy = EnergyLevel.High },
            new() { Name = "Elaborar repositorio", RequiredEnergy = EnergyLevel.MediumHigh},
            new() { Name = "Responder correos", RequiredEnergy = EnergyLevel.Medium },
            new() { Name = "Actualizar pendientes", RequiredEnergy = EnergyLevel.LowMedium},
            new() { Name = "Lavar platos", RequiredEnergy = EnergyLevel.Low }
        };

        // Act
        var result = TaskFilter.FilterByEnergy(tasks, EnergyLevel.LowMedium);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, t => t.Name == "Estudiar");
        Assert.DoesNotContain(result, t => t.Name == "Elaborar repositorio");
        Assert.DoesNotContain(result, t => t.Name == "Responder correos");
    }
    
    [Fact]
    public void FilterByEnergy_LowEnergy_DiscardsAllButLowTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new() { Name = "Estudiar", RequiredEnergy = EnergyLevel.High },
            new() { Name = "Elaborar repositorio", RequiredEnergy = EnergyLevel.MediumHigh},
            new() { Name = "Responder correos", RequiredEnergy = EnergyLevel.Medium },
            new() { Name = "Actualizar pendientes", RequiredEnergy = EnergyLevel.LowMedium},
            new() { Name = "Lavar platos", RequiredEnergy = EnergyLevel.Low }
        };

        // Act
        var result = TaskFilter.FilterByEnergy(tasks, EnergyLevel.Low);

        // Assert
        Assert.Single(result);
        Assert.DoesNotContain(result, t => t.Name == "Estudiar");
        Assert.DoesNotContain(result, t => t.Name == "Elaborar repositorio");
        Assert.DoesNotContain(result, t => t.Name == "Responder correos");
        Assert.DoesNotContain(result, t => t.Name == "Actualizar pendientes");
    }

    [Fact]
    public void FilterByEnergy_EmptyList_ResultIsAnEmptyList()
    {
        // Arrange
        var tasks = new List<TaskItem>();
        
        // Act
        var result = TaskFilter.FilterByEnergy(tasks, EnergyLevel.High);
        
        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterByEnergy_LowEnergy_DoesNotModifyOriginalList()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new() { Name = "Estudiar", RequiredEnergy = EnergyLevel.High },
            new() { Name = "Responder correos", RequiredEnergy = EnergyLevel.Medium },
            new() { Name = "Lavar platos", RequiredEnergy = EnergyLevel.Low }
        };
        var snapshot = tasks.ToList();

        // Act
        var result = TaskFilter.FilterByEnergy(tasks, EnergyLevel.Low);

        // Assert
        Assert.Equal(3, tasks.Count);
        Assert.Equal(snapshot, tasks);
        Assert.NotSame(tasks, result);
    }
}