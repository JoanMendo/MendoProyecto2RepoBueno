using UnityEngine;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Ingrediente que duplica las ganancias de dinero si no tiene ingredientes vecinos.
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Cebolla", menuName = "CookingGame/Resources/Ingredients/Cebolla")]
public class Cebolla : IngredientesSO
{
    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Buscar componente Economia en el cliente dueño
        Economia economiaJugador = FindObjectOfType<Economia>();
        if (economiaJugador == null)
        {
            Debug.LogError("No se encontró componente Economia para el efecto Cebolla");
            return;
        }

        // Verificar si todos los nodos vecinos están vacíos
        bool limpio = true;
        foreach (var vecino in nodosAfectados)
        {
            Node nodoVecino = vecino.GetComponent<Node>();
            if (nodoVecino != null && nodoVecino.hasIngredient.Value)
            {
                limpio = false;
                break;
            }
        }

        // Aplicar efecto de multiplicador
        if (limpio)
        {
            economiaJugador.SetMultiplicador(2);
            Debug.Log("Efecto cebolla activado: Ganancias duplicadas");
        }
        else
        {
            economiaJugador.SetMultiplicador(1);
            Debug.Log("Efecto cebolla desactivado: Ganancias normales");
        }
    }
}
