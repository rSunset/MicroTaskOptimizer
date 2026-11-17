using MicroTaskOptimizer.Core.Models;

namespace MicroTaskOptimizer.Core.Scheduling;

public static class TaskFilter
{
    public static List<TaskItem> FilterByEnergy(IEnumerable<TaskItem> tasks, EnergyLevel currentEnergy)
    {
        return tasks.Where(task => task.RequiredEnergy <= currentEnergy).ToList();
        /*
         * No se hace un .Remove() para evitar discrepancias al leer el enumerador.
         * Esto sucede por un mecanismo de List<T>
         *  La lista mantiene un contador de versión interno que aumenta cada vez que la modificas (Add, Remove, etc.).
         *  El foreach crea un enumerador que recuerda la versión con la que empezó.
         *  En cada paso comprueba si la versión cambió, y si es así lanza InvalidOperationException (“Collection was modified”).
         * Es una protección deliberada: si borraras elementos mientras recorres, los índices se desplazarían y te saltarías elementos sin darte cuenta.
         * Es mejor que falle de forma ruidosa. Tu solución, que construye una lista nueva sin tocar la original, evita el problema de raíz.
         */
    }
}