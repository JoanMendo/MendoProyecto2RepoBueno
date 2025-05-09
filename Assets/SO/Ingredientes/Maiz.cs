using UnityEngine;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que genera ingresos adicionales cada turno.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Maiz", menuName = "CookingGame/Resources/Ingredients/Maiz")]
public class Maiz : IngredientesSO
{
    [Tooltip("Cantidad de dinero generada por turno")]
    public float cantidadDinero = 10f;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Buscar componente Economia en el cliente dueño
        Economia economiaJugador = FindObjectOfType<Economia>();
        if (economiaJugador == null)
        {
            Debug.LogError("No se encontró componente Economia para el efecto Maiz");
            return;
        }

        // Generar dinero
        economiaJugador.more_money(cantidadDinero);
        Debug.Log($"Maiz generó {cantidadDinero} monedas");
    }
}