using UnityEngine;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que duplica las ganancias de dinero si no tiene ingredientes vecinos.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Cebolla", menuName = "CookingGame/Resources/Ingredients/Cebolla")]
public class Cebolla : IngredientesSO
{
    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        Debug.Log("Ejecutando efecto de Cebolla");

        // Obtener la referencia al NodeMap desde el nodo
        Node nodoComponente = nodoOrigen.GetComponent<Node>();
        if (nodoComponente == null || nodoComponente.nodeMap == null)
        {
            Debug.LogError("No se pudo obtener NodeMap desde el nodo origen");
            return;
        }

        // Obtener ID del propietario del tablero
        ulong clientId = nodoComponente.nodeMap.ownerClientId;

        // Usar el IngredientManager para crear el gestor de efecto
        if (IngredientManager.Instance != null)
        {
            GameObject gestorObj = IngredientManager.Instance.CrearGestorEfecto("cebolla");
            if (gestorObj != null)
            {
                IEffectManager gestor = gestorObj.GetComponent<IEffectManager>();
                if (gestor != null)
                {
                    // Configurar el gestor con este ingrediente
                    gestor.ConfigurarConIngrediente(this);

                    // Iniciar el efecto usando la interfaz estandarizada
                    gestor.IniciarEfecto(nodoOrigen, nodosAfectados);
                }
                else
                {
                    Debug.LogError("El gestor de efectos de cebolla no implementa IEffectManager");
                    Destroy(gestorObj);
                }
            }
        }
        else
        {
            Debug.LogError("No se encontró IngredientManager para el efecto Cebolla");
        }
    }
}
