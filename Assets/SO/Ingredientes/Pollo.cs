using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que rota los ingredientes vecinos en sentido horario.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Pollo", menuName = "CookingGame/Resources/Ingredients/Pollo")]
public class Pollo : IngredientesSO
{
    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        Debug.Log("Ejecutando efecto de Pollo");

        // Usar el IngredientManager para crear el gestor de efecto
        if (IngredientManager.Instance != null)
        {
            GameObject gestorObj = IngredientManager.Instance.CrearGestorEfecto("pollo");
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
                    Debug.LogError("El gestor de efectos de pollo no implementa IEffectManager");
                    Destroy(gestorObj);
                }
            }
        }
        else
        {
            Debug.LogError("No se encontró IngredientManager para el efecto Pollo");
        }
    }
}



