
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Representa un utensilio que puede colocarse en el tablero.
/// Los utensilios generalmente afectan a varios nodos y tienen efectos potentes.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "New Utensilio", menuName = "CookingGame/Resources/Utensilio")]
public class Utensilio : ResourcesSO
{
    [Header("Propiedades de Utensilio")]
    [Tooltip("Número de nodos que requiere este utensilio")]
    [Min(1)]
    public int nodosRequeridos = 1;

    [Tooltip("Tipo de utensilio (corte, cocción, etc.)")]
    public string tipoUtensilio;

    [Tooltip("Potencia del efecto del utensilio")]
    [Range(1, 10)]
    public int potencia = 1;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Activa el efecto específico del utensilio
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public override void ActivarEfecto(GameObject nodoOrigen, NodeMap nodeMap)
    {
        // Para utensilios que ocupan múltiples nodos, podemos necesitar lógica adicional
        if (nodosRequeridos > 1)
        {
            // Esta implementación asume que ya se ha validado previamente 
            // que hay nodos suficientes y son válidos
            Debug.Log($"Utensilio {Name} activado en múltiples nodos");
        }

        // Calcular nodos afectados
        List<GameObject> nodosAfectados = CalcularNodosAfectados(nodoOrigen, nodeMap);

        // Aplicar efecto específico a los nodos afectados
        AplicarEfectoEspecifico(nodoOrigen, nodosAfectados);

        // Log para debugging
        Debug.Log($"Utensilio {Name} activó su efecto desde {nodoOrigen.name} afectando a {nodosAfectados.Count} nodos");
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Método para que las subclases implementen efectos específicos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    protected virtual void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Implementación base vacía - clases hijas sobreescriben según necesidad
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Valida si un conjunto de nodos puede albergar este utensilio
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public virtual bool ValidarColocacion(List<GameObject> nodos)
    {
        // Verificación básica de cantidad
        if (nodos.Count != nodosRequeridos)
        {
            return false;
        }

        // Verificar que los nodos estén vacíos
        foreach (var nodo in nodos)
        {
            Node componenteNodo = nodo.GetComponent<Node>();
            if (componenteNodo == null || componenteNodo.hasIngredient.Value)
            {
                return false;
            }
        }

        // Para utensilios más complejos, las subclases pueden agregar validaciones adicionales
        return true;
    }
}
