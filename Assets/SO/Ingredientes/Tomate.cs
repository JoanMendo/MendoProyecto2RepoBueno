using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que permite moverse a un nodo adyacente vacío.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Tomate", menuName = "CookingGame/Resources/Ingredients/Tomate")]
public class Tomate : IngredientesSO
{
    [Tooltip("Material para resaltar nodos disponibles para movimiento")]
    public Material materialResaltado;

    [Tooltip("Color para resaltar nodos disponibles")]
    public Color colorResaltado = Color.red;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        Debug.Log("Ejecutando efecto de Tomate");

        // Usar el IngredientManager para crear el gestor de efecto
        if (IngredientManager.Instance != null)
        {
            GameObject gestorObj = IngredientManager.Instance.CrearGestorEfecto("tomate");
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
                    Debug.LogError("El gestor de efectos de tomate no implementa IEffectManager");
                    Destroy(gestorObj);
                }
            }
        }
        else
        {
            Debug.LogError("No se encontró IngredientManager para el efecto Tomate");
        }
    }
}

