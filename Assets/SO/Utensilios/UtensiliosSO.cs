
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// ��<summary>_PLACEHOLDER��
/// Representa un utensilio que puede colocarse en el tablero.
/// Los utensilios generalmente afectan a varios nodos y tienen efectos potentes.
/// ��</summary>_PLACEHOLDER��
[CreateAssetMenu(fileName = "New Utensilio", menuName = "CookingGame/Resources/Utensilio")]
public class Utensilio : ResourcesSO
{
    [Header("Propiedades de Utensilio")]
    [Tooltip("N�mero de nodos que requiere este utensilio")]
    [Min(1)]
    public int nodosRequeridos = 1;

    [Tooltip("Tipo de utensilio (corte, cocci�n, etc.)")]
    public string tipoUtensilio;

    [Tooltip("Potencia del efecto del utensilio")]
    [Range(1, 10)]
    public int potencia = 1;

    /// ��<summary>_PLACEHOLDER��
    /// Activa el efecto espec�fico del utensilio
    /// ��</summary>_PLACEHOLDER��
    public override void ActivarEfecto(GameObject nodoOrigen, NodeMap nodeMap)
    {
        // Para utensilios que ocupan m�ltiples nodos, podemos necesitar l�gica adicional
        if (nodosRequeridos > 1)
        {
            // Esta implementaci�n asume que ya se ha validado previamente 
            // que hay nodos suficientes y son v�lidos
            Debug.Log($"Utensilio {Name} activado en m�ltiples nodos");
        }

        // Calcular nodos afectados
        List<GameObject> nodosAfectados = CalcularNodosAfectados(nodoOrigen, nodeMap);

        // Aplicar efecto espec�fico a los nodos afectados
        AplicarEfectoEspecifico(nodoOrigen, nodosAfectados);

        // Log para debugging
        Debug.Log($"Utensilio {Name} activ� su efecto desde {nodoOrigen.name} afectando a {nodosAfectados.Count} nodos");
    }

    /// ��<summary>_PLACEHOLDER��
    /// M�todo para que las subclases implementen efectos espec�ficos
    /// ��</summary>_PLACEHOLDER��
    protected virtual void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Implementaci�n base vac�a - clases hijas sobreescriben seg�n necesidad
    }

    /// ��<summary>_PLACEHOLDER��
    /// Valida si un conjunto de nodos puede albergar este utensilio
    /// ��</summary>_PLACEHOLDER��
    public virtual bool ValidarColocacion(List<GameObject> nodos)
    {
        // Verificaci�n b�sica de cantidad
        if (nodos.Count != nodosRequeridos)
        {
            return false;
        }

        // Verificar que los nodos est�n vac�os
        foreach (var nodo in nodos)
        {
            Node componenteNodo = nodo.GetComponent<Node>();
            if (componenteNodo == null || componenteNodo.hasIngredient.Value)
            {
                return false;
            }
        }

        // Para utensilios m�s complejos, las subclases pueden agregar validaciones adicionales
        return true;
    }
}
