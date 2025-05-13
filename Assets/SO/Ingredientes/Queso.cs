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
        Debug.Log("Ejecutando efecto de Queso");

        // Verificar si hay nodos vecinos afectados
        if (nodosAfectados.Count == 0) return;

        // Usar el IngredientManager para crear el gestor de efecto
        if (IngredientManager.Instance != null)
        {
            GameObject gestorObj = IngredientManager.Instance.CrearGestorEfecto("queso");
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
                    Debug.LogError("El gestor de efectos de queso no implementa IEffectManager");
                    Destroy(gestorObj);
                }
            }
        }
        else
        {
            Debug.LogError("No se encontró IngredientManager para el efecto Queso");
        }
    }
}
