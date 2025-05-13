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
        Debug.Log("Ejecutando efecto de Oliva_V");

        // Verificar si hay nodos afectados
        if (nodosAfectados.Count == 0) return;

        // Usar el IngredientManager para crear el gestor de efecto
        if (IngredientManager.Instance != null)
        {
            GameObject gestorObj = IngredientManager.Instance.CrearGestorEfecto("olivav");
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
                    Debug.LogError("El gestor de efectos de Oliva_V no implementa IEffectManager");
                    Destroy(gestorObj);
                }
            }
        }
        else
        {
            Debug.LogError("No se encontró IngredientManager para el efecto Oliva_V");
        }
    }
}
