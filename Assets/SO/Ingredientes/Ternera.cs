using UnityEngine;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que aumenta la vida y hace inmóvil al primer vecino.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Ternera", menuName = "CookingGame/Resources/Ingredients/Ternera")]
public class Ternera : IngredientesSO
{
    [Tooltip("Cantidad de vida a aumentar")]
    public float aumentoVida = 1f;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        Debug.Log("Ejecutando efecto de Ternera");

        // Verificar si hay al menos un nodo vecino
        if (nodosAfectados.Count == 0) return;

        // Tomar el primer nodo de la lista
        GameObject primerVecino = nodosAfectados[0];
        Node nodoVecino = primerVecino.GetComponent<Node>();

        if (nodoVecino != null && nodoVecino.hasIngredient.Value && nodoVecino.currentIngredient != null)
        {
            // Obtener el ingrediente del nodo vecino
            ResourcesSO recursoVecino = nodoVecino.currentIngredient.GetComponent<ResourcesSO>();
            if (recursoVecino != null)
            {
                // Aplicar modificadores
                ModificadorRecurso modificador = nodoVecino.currentIngredient.GetComponent<ModificadorRecurso>();
                if (modificador == null)
                {
                    modificador = nodoVecino.currentIngredient.AddComponent<ModificadorRecurso>();
                }

                modificador.AumentarVida(aumentoVida);
                modificador.HacerInmovil();

                Debug.Log($"Ternera aumentó la vida de {recursoVecino.Name} en {aumentoVida} y lo hizo inmóvil");
            }
        }
    }
}