using MicroTaskOptimizer.Core.Models;

namespace MicroTaskOptimizer.Core.Scheduling;

/// <summary>
/// Operaciones de filtrado sobre tareas. Todas son funciones puras: no modifican
/// sus argumentos ni guardan estado entre llamadas.
/// </summary>
public static class TaskFilter
{
    ///<summary>
    /// Devuelve una lista nueva con las tareas cuya energía requerida no supera
    /// la energía actual. La colección recibida no se modifica.
    /// </summary>
    /// <remarks>
    /// La comparación usa los valores numéricos de <see cref="EnergyLevel"/>,
    /// por lo que el enum debe mantenerse ordenado de menor a mayor energía.
    /// </remarks>
    public static List<TaskItem> FilterByEnergy(IEnumerable<TaskItem> tasks, EnergyLevel currentEnergy)
    {
        return tasks.Where(task => task.RequiredEnergy <= currentEnergy).ToList();
    }
    
    #region NuggetsOfWisdom

        // NUGGET 1: por qué la función es pura
        //   Where + ToList crea una lista nueva y 'tasks' queda intacta. Una versión que
        //   hiciera RemoveAll sobre la entrada devolvería lo mismo, pero destruiría la lista
        //   del llamador. Lo detecta la prueba FilterByEnergy_LowEnergy_DoesNotModifyOriginalList.
        
        
        // NUGGET 2: por qué no se borra mientras se recorre
        //   List<T> mantiene un contador de versión que sube con cada Add/Remove. El foreach
        //   crea un enumerador que recuerda la versión inicial y la compara en cada paso:
        //   si cambió, lanza InvalidOperationException ("Collection was modified").
        //   Es una protección deliberada: al borrar, los índices se desplazan y se saltarían
        //   elementos sin aviso. Es mejor que falle de forma ruidosa.

    #endregion
}