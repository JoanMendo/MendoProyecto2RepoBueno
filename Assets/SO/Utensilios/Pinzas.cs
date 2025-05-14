using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Utensilio que mueve un ingrediente de un nodo a otro
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Pinzas", menuName = "CookingGame/Resources/Utensilios/Pinzas")]
public class Pinzas : Utensilio
{
    [Tooltip("Prefab para efecto visual de movimiento")]
    public GameObject prefabEfectoMovimiento;

    // Ya no necesitamos sobreescribir AplicarEfectoEspecifico porque 
    // ahora UtensiliosManager se encarga de crear el manager y ejecutar la acción

    // El método ValidarColocacion aún es útil y podemos mantenerlo si queremos
    // pero ahora la lógica de validación principal está en el PinzasManager
    public override bool ValidarColocacion(List<GameObject> nodos)
    {
        if (nodos.Count != 2) return false;

        // Verificar que el primer nodo tenga ingrediente y el segundo esté vacío
        Node nodo1 = nodos[0].GetComponent<Node>();
        Node nodo2 = nodos[1].GetComponent<Node>();

        if (nodo1 == null || nodo2 == null) return false;

        return nodo1.hasIngredient.Value && !nodo2.hasIngredient.Value;
    }
}

