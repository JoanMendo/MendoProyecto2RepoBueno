using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Utensilio que intercambia los ingredientes de dos nodos
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Espatula", menuName = "CookingGame/Resources/Utensilios/Espatula")]
public class Espatula : Utensilio
{
    [Tooltip("Prefab para efecto visual de intercambio")]
    public GameObject prefabEfectoIntercambio;

    // Ya no necesitamos sobreescribir AplicarEfectoEspecifico porque 
    // ahora UtensiliosManager se encarga de crear el manager y ejecutar la acción

    // Mantenemos el método ValidarColocacion para facilitar verificaciones rápidas
    public override bool ValidarColocacion(List<GameObject> nodos)
    {
        if (nodos.Count != 2) return false;

        // Verificar que ambos nodos tengan ingrediente
        Node nodo1 = nodos[0].GetComponent<Node>();
        Node nodo2 = nodos[1].GetComponent<Node>();

        if (nodo1 == null || nodo2 == null) return false;

        return nodo1.hasIngredient.Value && nodo2.hasIngredient.Value;
    }
}

