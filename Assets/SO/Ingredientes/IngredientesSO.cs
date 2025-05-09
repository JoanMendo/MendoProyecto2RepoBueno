
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Representa un ingrediente que puede colocarse en el tablero.
/// Los ingredientes pueden tener efectos pasivos o ser parte de recetas.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "New Ingredient", menuName = "CookingGame/Resources/Ingredient")]
public class IngredientesSO : ResourcesSO
{
    [Header("Propiedades de Ingrediente")]
    [Tooltip("Categoría del ingrediente (verdura, carne, etc.)")]
    public string categoria;

    [Tooltip("Valor nutricional del ingrediente")]
    public float valorNutricional;

    [Tooltip("Si este ingrediente es básico o procesado")]
    public bool esBasico = true;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Activa el efecto específico del ingrediente
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public override void ActivarEfecto(GameObject nodoOrigen, NodeMap nodeMap)
    {
        // Calcular nodos afectados según forma y rango
        List<GameObject> nodosAfectados = CalcularNodosAfectados(nodoOrigen, nodeMap);

        // Aplicar efecto específico a los nodos afectados
        AplicarEfectoEspecifico(nodoOrigen, nodosAfectados);

        // Log para debugging
        Debug.Log($"Ingrediente {Name} activó su efecto desde {nodoOrigen.name} afectando a {nodosAfectados.Count} nodos");
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Método protegido para que las subclases implementen efectos específicos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    protected virtual void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Implementación base vacía - clases hijas sobreescriben según necesidad
    }
}
