using UnityEngine;
using System.Collections.Generic;

/// ��<summary>_PLACEHOLDER��
/// Ingrediente que genera ingresos adicionales cada turno.
/// ��</summary>_PLACEHOLDER��
[CreateAssetMenu(fileName = "Maiz", menuName = "CookingGame/Resources/Ingredients/Maiz")]
public class Maiz : IngredientesSO
{
    [Tooltip("Cantidad de dinero generada por turno")]
    public float cantidadDinero = 10f;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        Debug.Log("Ejecutando efecto de Ma�z");

        // Obtener la referencia al NodeMap desde el nodo
        Node nodoComponente = nodoOrigen.GetComponent<Node>();
        if (nodoComponente == null || nodoComponente.nodeMap == null)
        {
            Debug.LogError("No se pudo obtener NodeMap desde el nodo origen");
            return;
        }

        // Usar el IngredientManager para crear el gestor de efecto
        if (IngredientManager.Instance != null)
        {
            GameObject gestorObj = IngredientManager.Instance.CrearGestorEfecto("maiz");
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
                    Debug.LogError("El gestor de efectos de ma�z no implementa IEffectManager");
                    Destroy(gestorObj);
                }
            }
        }
        else
        {
            Debug.LogError("No se encontr� IngredientManager para el efecto Ma�z");
        }
    }
}