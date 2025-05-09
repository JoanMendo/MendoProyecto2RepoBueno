
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Representa un efecto especial que puede activarse en el tablero.
/// Los efectos pueden ser temporales o permanentes.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "New Efecto", menuName = "CookingGame/Resources/Efecto")]
public class Efectos : ResourcesSO
{
    [Header("Propiedades de Efecto")]
    [Tooltip("Duración del efecto en turnos (-1 para permanente)")]
    public int duracion = 1;

    [Tooltip("Tipo de efecto (buff, debuff, transformación, etc.)")]
    public string tipoEfecto;

    [Tooltip("Si el efecto se activa inmediatamente o al final del turno")]
    public bool activacionInmediata = true;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Activa el efecto específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public override void ActivarEfecto(GameObject nodoOrigen, NodeMap nodeMap)
    {
        // Calcular nodos afectados
        List<GameObject> nodosAfectados = CalcularNodosAfectados(nodoOrigen, nodeMap);

        // Aplicar efecto específico
        AplicarEfectoEspecifico(nodoOrigen, nodosAfectados);

        // Log para debugging
        Debug.Log($"Efecto {Name} activado desde {nodoOrigen.name} afectando a {nodosAfectados.Count} nodos por {duracion} turnos");
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Método para que las subclases implementen efectos específicos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    protected virtual void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Implementación base vacía - clases hijas sobreescriben según necesidad
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Método que se ejecuta cada turno mientras el efecto esté activo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public virtual void EjecutarTurno(GameObject nodoAfectado)
    {
        // Por defecto no hace nada - clases derivadas implementan efectos por turno
        Debug.Log($"Efecto {Name} en ejecución sobre {nodoAfectado.name}");
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Método que se ejecuta cuando el efecto termina (por duración o cancelación)
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public virtual void FinalizarEfecto(GameObject nodoAfectado)
    {
        Debug.Log($"Efecto {Name} finalizado en {nodoAfectado.name}");
    }
}
