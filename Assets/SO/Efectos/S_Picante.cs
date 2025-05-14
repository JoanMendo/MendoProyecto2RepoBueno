using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Efecto que mueve ingredientes a posiciones aleatorias cada turno
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "S_Picante", menuName = "CookingGame/Resources/Efectos/S_Picante")]
public class S_Picante : Efectos
{
    public override void Activar(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Llamar al método base
        base.Activar(nodoObjetivo, mapa);

        // Ya no creamos el gestor aquí, lo hace EfectosManager
        Debug.Log($"Efecto picante activado desde {nodoObjetivo.name}");
    }

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Este método está vacío porque la funcionalidad se implementa en el gestor
        // EfectoPicanteManager ahora se crea desde EfectosManager
    }
}

