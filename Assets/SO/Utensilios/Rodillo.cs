using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Utensilio que mueve todos los ingredientes de una columna hacia un lado
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Rodillo", menuName = "CookingGame/Resources/Utensilios/Rodillo")]
public class Rodillo : Utensilio
{
    [Tooltip("Prefab para efecto visual de movimiento")]
    public GameObject prefabEfectoMovimiento;

    [Tooltip("Dirección del movimiento (true = derecha, false = izquierda)")]
    public bool moverDerecha = true;

    // Ya no necesitamos sobreescribir AplicarEfectoEspecifico porque 
    // ahora UtensiliosManager se encarga de crear el manager y ejecutar la acción
}
