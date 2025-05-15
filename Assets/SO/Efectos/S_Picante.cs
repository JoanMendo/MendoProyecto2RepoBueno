using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Efecto que mueve ingredientes a posiciones aleatorias cada turno
/// </summary>
[CreateAssetMenu(fileName = "S_Picante", menuName = "CookingGame/Resources/Efectos/S_Picante")]
public class S_Picante : Efectos
{
    // Añadir una propiedad explícita para indicar que este efecto está vinculado al nodo, no al ingrediente
    [Tooltip("Este efecto está vinculado a nodos, no a ingredientes")]
    public bool efectoVinculadoANodo = true;

    public override void Activar(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Llamar al método base
        base.Activar(nodoObjetivo, mapa);

        Debug.Log($"Efecto picante activado desde {nodoObjetivo.name}");
    }

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Este método está vacío porque la funcionalidad se implementa en el gestor
    }
}

