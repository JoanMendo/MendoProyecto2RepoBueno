using UnityEngine;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que disminuye el rango de los ingredientes vecinos.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Oliva_V", menuName = "CookingGame/Resources/Ingredients/Oliva_V")]
public class Oliva_V : IngredientesSO
{
    [Tooltip("Cantidad de rango a reducir")]
    public int reduccionRango = 1;

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
                // Reducir rango mediante un componente modificador
                ModificadorRecurso modificador = nodo.currentIngredient.GetComponent<ModificadorRecurso>();
                if (modificador == null)
                {
                    modificador = nodo.currentIngredient.AddComponent<ModificadorRecurso>();
                }

                modificador.DisminuirRango(reduccionRango);
                Debug.Log($"Oliva V disminuyó el rango de {recursoVecino.Name} en {reduccionRango}");
            }
        }
    }
}
