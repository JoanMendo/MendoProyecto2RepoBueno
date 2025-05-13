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
        Debug.Log("Ejecutando efecto de Oliva_N");

        // Verificar si hay nodos afectados
        if (nodosAfectados.Count == 0) return;

        // Usar el IngredientManager para crear el gestor de efecto
        if (IngredientManager.Instance != null)
        {
            GameObject gestorObj = IngredientManager.Instance.CrearGestorEfecto("olivan");
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
                    Debug.LogError("El gestor de efectos de Oliva_N no implementa IEffectManager");
                    Destroy(gestorObj);
                }
            }
        }
        else
        {
            Debug.LogError("No se encontró IngredientManager para el efecto Oliva_N");
        }
    }
}