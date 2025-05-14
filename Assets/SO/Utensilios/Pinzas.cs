using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// ��<summary>_PLACEHOLDER��
/// Utensilio que mueve un ingrediente de un nodo a otro
/// ��</summary>_PLACEHOLDER��
[CreateAssetMenu(fileName = "Pinzas", menuName = "CookingGame/Resources/Utensilios/Pinzas")]
public class Pinzas : Utensilio
{
    [Tooltip("Prefab para efecto visual de movimiento")]
    public GameObject prefabEfectoMovimiento;

    // Ya no necesitamos sobreescribir AplicarEfectoEspecifico porque 
    // ahora UtensiliosManager se encarga de crear el manager y ejecutar la acci�n

    // El m�todo ValidarColocacion a�n es �til y podemos mantenerlo si queremos
    // pero ahora la l�gica de validaci�n principal est� en el PinzasManager
    public override bool ValidarColocacion(List<GameObject> nodos)
    {
        if (nodos.Count != 2) return false;

        // Verificar que el primer nodo tenga ingrediente y el segundo est� vac�o
        Node nodo1 = nodos[0].GetComponent<Node>();
        Node nodo2 = nodos[1].GetComponent<Node>();

        if (nodo1 == null || nodo2 == null) return false;

        return nodo1.hasIngredient.Value && !nodo2.hasIngredient.Value;
    }
}

