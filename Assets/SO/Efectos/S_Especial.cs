using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Efecto que mueve todos los ingredientes de una fila hacia arriba
/// </summary>
[CreateAssetMenu(fileName = "S_Especial", menuName = "CookingGame/Resources/Efectos/S_Especial")]
public class S_Especial : Efectos
{
    [Tooltip("Fuerza del empuje aplicado a los ingredientes")]
    [Range(1f, 3f)]
    public float fuerzaEmpuje = 1f;

    [Tooltip("Prefab para efecto visual de empuje")]
    public GameObject prefabEfectoEmpuje;

    [Tooltip("Color del efecto visual")]
    public Color colorEfecto = Color.green;

    /// <summary>
    /// Activa el efecto en el nodo especificado
    /// </summary>
    public override void Activar(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Llamar al método base para mantener compatibilidad
        base.Activar(nodoObjetivo, mapa);

        Debug.Log($"Efecto especial activado desde {nodoObjetivo.name}");
    }

    /// <summary>
    /// Método específico para aplicar el efecto especial
    /// </summary>
    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Este método está vacío porque la funcionalidad se implementa en el gestor EfectoEspecialManager
        // El EfectoManager crea el gestor a través de EfectosManager
    }

    /// <summary>
    /// Calcula los nodos afectados por este efecto
    /// Para S_Especial, todos los nodos en las cuatro direcciones cardinales
    /// </summary>
    public override List<GameObject> CalcularNodosAfectados(GameObject nodoObjetivo, NodeMap mapa)
    {
        if (nodoObjetivo == null || mapa == null)
        {
            return new List<GameObject>();
        }

        Node nodoOrigen = nodoObjetivo.GetComponent<Node>();
        if (nodoOrigen == null)
        {
            return new List<GameObject>();
        }

        Vector2Int posOrigen = nodoOrigen.position;
        List<GameObject> nodosAfectados = new List<GameObject>();

        // Direcciones cardinales
        Vector2Int[] direcciones = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Arriba
            new Vector2Int(1, 0),   // Derecha
            new Vector2Int(0, -1),  // Abajo
            new Vector2Int(-1, 0)   // Izquierda
        };

        // Para cada dirección, añadir todos los nodos en esa línea
        foreach (Vector2Int dir in direcciones)
        {
            Vector2Int posCurrent = posOrigen + dir;

            // Avanzar en la dirección hasta el límite del tablero
            while (true)
            {
                GameObject nodoEnPos = mapa.GetNodeAtPosition(posCurrent);

                // Si llegamos al límite del tablero, parar
                if (nodoEnPos == null)
                    break;

                // Añadir nodo a la lista de afectados
                nodosAfectados.Add(nodoEnPos);

                // Avanzar al siguiente nodo en la dirección
                posCurrent += dir;
            }
        }

        return nodosAfectados;
    }
}