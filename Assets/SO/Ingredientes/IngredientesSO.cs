
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// ��<summary>_PLACEHOLDER��
/// Representa un ingrediente que puede colocarse en el tablero.
/// Los ingredientes pueden tener efectos pasivos o ser parte de recetas.
/// ��</summary>_PLACEHOLDER��
[CreateAssetMenu(fileName = "New Ingredient", menuName = "CookingGame/Resources/Ingredient")]
public class IngredientesSO : ResourcesSO
{
    [Header("Propiedades de Ingrediente")]
    [Tooltip("Categor�a del ingrediente (verdura, carne, etc.)")]
    public string categoria;

    [Tooltip("Valor nutricional del ingrediente")]
    public float valorNutricional;

    [Tooltip("Si este ingrediente es b�sico o procesado")]
    public bool esBasico = true;

    /// ��<summary>_PLACEHOLDER��
    /// Activa el efecto espec�fico del ingrediente
    /// ��</summary>_PLACEHOLDER��
    public override void ActivarEfecto(GameObject nodoOrigen, NodeMap nodeMap)
    {
        // Calcular nodos afectados seg�n forma y rango
        List<GameObject> nodosAfectados = CalcularNodosAfectados(nodoOrigen, nodeMap);

        // Aplicar efecto espec�fico a los nodos afectados
        AplicarEfectoEspecifico(nodoOrigen, nodosAfectados);

        // Log para debugging
        Debug.Log($"Ingrediente {Name} activ� su efecto desde {nodoOrigen.name} afectando a {nodosAfectados.Count} nodos");
    }

    /// ��<summary>_PLACEHOLDER��
    /// M�todo protegido para que las subclases implementen efectos espec�ficos
    /// ��</summary>_PLACEHOLDER��
    protected virtual void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Implementaci�n base vac�a - clases hijas sobreescriben seg�n necesidad
    }
}
