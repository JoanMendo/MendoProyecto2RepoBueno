using UnityEngine;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que impide que los ingredientes vecinos sean movidos.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Queso", menuName = "CookingGame/Resources/Ingredients/Queso")]
public class Queso : IngredientesSO
{
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
                // Aplicar modificador para hacer inmóvil
                ModificadorRecurso modificador = nodo.currentIngredient.GetComponent<ModificadorRecurso>();
                if (modificador == null)
                {
                    modificador = nodo.currentIngredient.AddComponent<ModificadorRecurso>();
                }

                modificador.HacerInmovil();
                Debug.Log($"Queso hizo inmóvil a {recursoVecino.Name}");
            }
        }
    }
}
