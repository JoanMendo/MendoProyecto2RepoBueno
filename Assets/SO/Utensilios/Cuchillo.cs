using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Utensilio que recorre nodos hacia arriba y destruye el primer ingrediente que encuentra
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Cuchillo", menuName = "CookingGame/Resources/Utensilios/Cuchillo")]
public class Cuchillo : Utensilio
{
    [Tooltip("Velocidad de movimiento del cuchillo")]
    public float velocidadMovimiento = 0.2f;

    [Tooltip("Prefab para efecto visual al cortar")]
    public GameObject prefabEfectoCorte;

    // Ya no necesitamos sobreescribir AplicarEfectoEspecifico porque 
    // ahora UtensiliosManager se encarga de crear el manager y ejecutar la acción
}


