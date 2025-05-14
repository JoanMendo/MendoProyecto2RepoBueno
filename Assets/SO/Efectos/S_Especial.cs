using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Efecto que mueve todos los ingredientes de una fila hacia arriba
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "S_Especial", menuName = "CookingGame/Resources/Efectos/S_Especial")]
public class S_Especial : Efectos
{
    public override void Activar(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Llamar al método base para mantener el comportamiento heredado
        base.Activar(nodoObjetivo, mapa);

        // Ya no necesitamos crear el gestor aquí, lo hace EfectosManager
        Debug.Log($"Efecto especial activado desde {nodoObjetivo.name}");
    }

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Este método está vacío porque la funcionalidad se implementa en el gestor
        // EfectoEspecialManager ahora se crea desde EfectosManager
    }
}

