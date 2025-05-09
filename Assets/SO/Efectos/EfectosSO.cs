
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// ��<summary>_PLACEHOLDER��
/// Representa un efecto especial que puede activarse en el tablero.
/// Los efectos pueden ser temporales o permanentes.
/// ��</summary>_PLACEHOLDER��
[CreateAssetMenu(fileName = "New Efecto", menuName = "CookingGame/Resources/Efecto")]
public class Efectos : ResourcesSO
{
    [Header("Propiedades de Efecto")]
    [Tooltip("Duraci�n del efecto en turnos (-1 para permanente)")]
    public int duracion = 1;

    [Tooltip("Tipo de efecto (buff, debuff, transformaci�n, etc.)")]
    public string tipoEfecto;

    [Tooltip("Si el efecto se activa inmediatamente o al final del turno")]
    public bool activacionInmediata = true;

    /// ��<summary>_PLACEHOLDER��
    /// Activa el efecto espec�fico
    /// ��</summary>_PLACEHOLDER��
    public override void ActivarEfecto(GameObject nodoOrigen, NodeMap nodeMap)
    {
        // Calcular nodos afectados
        List<GameObject> nodosAfectados = CalcularNodosAfectados(nodoOrigen, nodeMap);

        // Aplicar efecto espec�fico
        AplicarEfectoEspecifico(nodoOrigen, nodosAfectados);

        // Log para debugging
        Debug.Log($"Efecto {Name} activado desde {nodoOrigen.name} afectando a {nodosAfectados.Count} nodos por {duracion} turnos");
    }

    /// ��<summary>_PLACEHOLDER��
    /// M�todo para que las subclases implementen efectos espec�ficos
    /// ��</summary>_PLACEHOLDER��
    protected virtual void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Implementaci�n base vac�a - clases hijas sobreescriben seg�n necesidad
    }

    /// ��<summary>_PLACEHOLDER��
    /// M�todo que se ejecuta cada turno mientras el efecto est� activo
    /// ��</summary>_PLACEHOLDER��
    public virtual void EjecutarTurno(GameObject nodoAfectado)
    {
        // Por defecto no hace nada - clases derivadas implementan efectos por turno
        Debug.Log($"Efecto {Name} en ejecuci�n sobre {nodoAfectado.name}");
    }

    /// ��<summary>_PLACEHOLDER��
    /// M�todo que se ejecuta cuando el efecto termina (por duraci�n o cancelaci�n)
    /// ��</summary>_PLACEHOLDER��
    public virtual void FinalizarEfecto(GameObject nodoAfectado)
    {
        Debug.Log($"Efecto {Name} finalizado en {nodoAfectado.name}");
    }
}
