using UnityEngine;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que aumenta el rango de los ingredientes vecinos.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Oliva_N", menuName = "CookingGame/Resources/Ingredients/Oliva_N")]
public class Oliva_N : IngredientesSO
{
    [Tooltip("Cantidad de rango adicional")]
    public int aumentoRango = 1;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        foreach (var nodoVecino in nodosAfectados)
        {
            Node nodo = nodoVecino.GetComponent<Node>();
            if (nodo == null || !nodo.hasIngredient.Value || nodo.currentIngredient == null)
                continue;

            // Obtener el ingrediente del nodo vecino
            ResourcesSO recursoVecino = nodo.currentIngredient.GetComponent<ResourcesSO>();
            if (recursoVecino != null)
            {
                // Aumentar rango mediante un componente modificador
                ModificadorRecurso modificador = nodo.currentIngredient.GetComponent<ModificadorRecurso>();
                if (modificador == null)
                {
                    modificador = nodo.currentIngredient.AddComponent<ModificadorRecurso>();
                }

                modificador.AumentarRango(aumentoRango);
                Debug.Log($"Oliva N aumentó el rango de {recursoVecino.Name} en {aumentoRango}");
            }
        }
    }
}