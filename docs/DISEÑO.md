# Diseño: Optimizador de Micro-tareas

Estado: borrador v1 · Última actualización: 19 de noviembre de 2026

## 1. Objetivo

Aplicación de escritorio (Avalonia, C#, MVVM) que, al abrirse, propone un horario automático
para lo que resta del día. El usuario define la ventana del día (hora actual y hora de fin).
Cada tarea tiene duración, prioridad, energía requerida y, opcionalmente, un deadline.
El cálculo lo hace una función AWS Lambda (C# .NET) expuesta mediante API Gateway (REST API).

Reglas de negocio acordadas:

- Completar tareas pesa más que el estado de energía. La energía **penaliza**, no excluye.
- Una tarea de proyecto puede ser más urgente que una tarea simple (prioridad + cercanía del deadline).
- Si el usuario marca una tarea como no realizada o cambia su energía, el cliente envía una
  petición nueva con el estado actualizado y recibe un horario nuevo.

## 2. Arquitectura

```
Avalonia (cliente, fuente de verdad de las tareas)
   │  POST /schedule  (JSON)
   ▼
API Gateway (REST API, integración proxy)
   ▼
Lambda (MicroTaskOptimizer.Lambda): deserializa, valida, responde
   ▼
Core (MicroTaskOptimizer.Core): lógica pura, sin dependencias de AWS ni de UI
```

Decisiones:

- **Lambda sin estado.** Cada petición trae todo lo necesario; no se guarda nada entre llamadas.
- **La hora la envía el cliente** (`now`). Core nunca lee el reloj del sistema: así las funciones
  son puras y se pueden probar con una hora fija.
- **Fechas con zona horaria** (ISO 8601, `DateTimeOffset`).
- **El cliente guarda las tareas** (archivo local o SQLite). Guardado en la nube queda para después.
- **Solución con proyectos separados:** `Core`, `Lambda`, `Core.Tests` y, más adelante, `Desktop`.

## 3. Alcance de la versión 1

- Tareas con `id`, duración, prioridad (1 a 5), energía requerida (5 niveles) y deadline opcional.
- Ventana del día definida por `now` y `dayEnd` (máximo 24 h).
- Puntaje por tarea (prioridad, urgencia, energía como penalización).
- Selección óptima con programación dinámica (Mochila 0/1 con restricción de deadline).
- Algoritmo Greedy como línea base de comparación.
- Horario ordenado con hora de inicio y fin, tareas que no entraron con su motivo, y avisos.
- Reprogramación enviando el estado nuevo.

## 4. Para después

- Sub-tareas con dependencias (`parentId` opcional) y división automática en micro-tareas.
- Guardado y sincronización en la nube (autenticación, base de datos).
- Curva de energía a lo largo del día.
- Bloques ocupados (reuniones, comidas) y descansos.
- Tareas partibles en varios bloques.

## 5. Contrato de la petición

`POST /schedule`

```json
{
  "now": "2026-11-19T09:30:00-06:00",
  "dayEnd": "2026-11-19T21:00:00-06:00",
  "currentEnergy": "Medium",
  "tasks": [
    {
      "id": "a1f3c9e2",
      "name": "Entregar proyecto de algoritmos",
      "durationMinutes": 120,
      "priority": 5,
      "requiredEnergy": "High",
      "deadline": "2026-11-19T18:00:00-06:00"
    },
    {
      "id": "b7c24d10",
      "name": "Lavar platos",
      "durationMinutes": 15,
      "priority": 2,
      "requiredEnergy": "Low",
      "deadline": null
    }
  ]
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `now` | fecha con zona | La envía el cliente. Mantiene a Core puro y probable |
| `dayEnd` | fecha con zona | Debe ser posterior a `now`; la ventana no puede superar 1440 min |
| `currentEnergy` | enum | `Low`, `LowMedium`, `Medium`, `MediumHigh`, `High` |
| `tasks[].id` | string | Único. Permite referenciar la tarea en la respuesta |
| `tasks[].name` | string | No vacío |
| `tasks[].durationMinutes` | entero | Mayor que 0. Es el "peso" de la mochila |
| `tasks[].priority` | entero | De 1 a 5 |
| `tasks[].requiredEnergy` | enum | Mismos valores que `currentEnergy` |
| `tasks[].deadline` | fecha con zona o `null` | Opcional |

El enum de energía debe mantenerse ordenado de menor a mayor (`Low = 1` … `High = 5`), porque
la penalización usa la diferencia numérica entre niveles.

## 6. Contrato de la respuesta

`200 OK`, para la petición de ejemplo:

```json
{
  "schedule": [
    {
      "taskId": "a1f3c9e2",
      "name": "Entregar proyecto de algoritmos",
      "start": "2026-11-19T09:30:00-06:00",
      "end": "2026-11-19T11:30:00-06:00"
    },
    {
      "taskId": "b7c24d10",
      "name": "Lavar platos",
      "start": "2026-11-19T11:30:00-06:00",
      "end": "2026-11-19T11:45:00-06:00"
    }
  ],
  "unscheduled": [],
  "warnings": [
    { "taskId": "a1f3c9e2", "code": "EnergyBelowRequired" }
  ]
}
```

Una tarea que no entra aparece así:

```json
{ "taskId": "c3d4e5f6", "reason": "WouldMissDeadline" }
```

Códigos:

| Lista | Código | Significado |
|---|---|---|
| `unscheduled` | `DoesNotFit` | No hay tiempo suficiente en la ventana |
| `unscheduled` | `WouldMissDeadline` | Entra en la ventana, pero terminaría después de su deadline |
| `unscheduled` | `DeadlinePassed` | Su deadline es anterior a `now` |
| `warnings` | `EnergyBelowRequired` | Se programó aunque exige más energía que la actual |

Decisión: la API devuelve **códigos**, no texto. El cliente decide el idioma y el mensaje.

Errores de validación: `400 Bad Request` con `{ "errors": ["..."] }`.

## 7. Algoritmo

Todo vive en `Core` como funciones puras.

1. **Validación.** Reglas de la sección 5 y de la sección 8.
2. **Puntaje (valor entero por tarea).**
   `valor = round(prioridad × 10 × factorUrgencia × factorEnergía)`
    - Holgura = (deadline − now) − duración, en minutos.
    - `factorUrgencia`: holgura ≤ 60 → 2.0; ≤ 240 → 1.5; en otro caso, o sin deadline → 1.0.
    - `factorEnergía`: `max(0.4, 1 − 0.15 × max(0, requerida − actual))`. Nunca llega a 0,
      así la energía no puede excluir una tarea por sí sola.
    - Son valores iniciales, ajustables con pruebas.
3. **Selección.** Se ordenan las tareas por deadline (sin deadline = `dayEnd`). Programación
   dinámica sobre el tiempo disponible en minutos: `dp[t]` = valor máximo con tiempo total `t`.
   Una tarea solo se puede añadir si el tiempo acumulado, con ella, no supera su deadline.
   Es la Mochila 0/1 con una restricción de deadline.
4. **Línea base Greedy.** Ordenar por valor/duración y llenar mientras quepa y respete deadlines.
   Se compara con la programación dinámica sobre los mismos datos.
5. **Orden y horas.** Las tareas seleccionadas se colocan consecutivamente desde `now`, en orden
   de deadline y, a igualdad, mayor valor primero.
6. **Avisos.** Se añade `EnergyBelowRequired` a cada tarea programada cuya energía requerida
   supere la actual.

## 8. Casos límite (decisiones)

| Caso | Decisión |
|---|---|
| `now` ≥ `dayEnd`, o ventana mayor a 24 h | `400` |
| Deadline anterior a `now` | La tarea va a `unscheduled` con `DeadlinePassed` (no es un error del cliente) |
| Tarea más larga que la ventana disponible | `unscheduled` con `DoesNotFit` |
| `id` duplicado o `name` vacío | `400` |
| Duración ≤ 0, prioridad fuera de 1 a 5, enum no definido | `400` |
| Lista `tasks` vacía | `200` con horario vacío |

## 9. Estado de implementación

Hecho:

- Solución con proyectos `Core`, `Lambda` y `Core.Tests`, subida a GitHub.
- `Core`: `EnergyLevel` (5 niveles), `TaskItem`, `ScheduleRequest`, `TaskFilter` (con pruebas).
- `Lambda`: handler que deserializa el cuerpo y responde `400` con JSON válido.

Pendiente, por prioridad:

| # | Pieza | Nota |
|---|---|---|
| 1 | Actualizar modelos al contrato (`Id`, `Deadline`, `Now`, `DayEnd`) | Es el cambio que desbloquea todo lo demás |
| 2 | Mochila 0/1 clásica con pruebas | Núcleo de la materia |
| 3 | Puntaje (prioridad, urgencia, energía) | Reemplaza al filtro duro de energía |
| 4 | Restricción de deadline en la selección | Extensión de la Mochila |
| 5 | Orden y asignación de horas | |
| 6 | Greedy y comparación | Material para el análisis |
| 7 | Validador y conexión con el handler | |
| 8 | Despliegue en AWS | Depende de la cuenta y los permisos |
| 9 | Interfaz Avalonia (MVVM) | |

## 10. Plan para el viernes

El orden de la tabla anterior es el orden de recorte: si falta tiempo, se sacrifica desde abajo.
Un entregable demostrable con las piezas 1 a 7 es una Lambda cuyo handler se puede invocar
localmente con un JSON de ejemplo, con el algoritmo cubierto por pruebas. El despliegue en AWS y
la interfaz serían un segundo paso.