using System.Collections.Generic;
using UnityEngine;


public class ComprobadorVictoria : MonoBehaviour
{
    public NodeMap nodeMap;


    // Receta: IngredienteSO -> cantidad mínima requerida
    public List<IngredienteCantidad> recetaMinimos = new List<IngredienteCantidad>();

    [System.Serializable]
    public class IngredienteCantidad
    {
        public IngredientesSO ingrediente;
        public int cantidadTotal;
    }

    public void ComprobarVictoria()
    {
        List<IngredienteCantidad> conteoJugador = ContarIngredientes(nodeMap);
        bool cumpleJugador = CumpleReceta(conteoJugador);
        int valorSobrante = CalcularValorSobrante(conteoJugador);  
    }

    private List<IngredienteCantidad> ContarIngredientes(NodeMap nodeMap)
    {
        List<IngredienteCantidad> conteo = new List<IngredienteCantidad>();

        foreach (GameObject node in nodeMap.nodesList)
        {
            Node nodeComponent = node.GetComponent<Node>();
            if (nodeComponent.hasIngredient)
            {
                GameObject ingrediente = nodeComponent.currentIngredient;
                IngredientesSO ingredienteSO = ingrediente.GetComponent<IngredientesSO>(); 

                if (conteo.Exists(x => x.ingrediente == ingredienteSO))
                {
                    // Si ya existe, aumentar la cantidad
                    IngredienteCantidad item = conteo.Find(x => x.ingrediente == ingredienteSO);
                    item.cantidadTotal++;
                }
                else
                {
                    conteo.Add(new IngredienteCantidad { ingrediente = ingredienteSO, cantidadTotal = 1 });
                }
                
            }
        }

        return conteo;
    }

    private bool CumpleReceta(List<IngredienteCantidad> conteo)
    {
        foreach (IngredienteCantidad item in recetaMinimos)
        {
           if (conteo.Contains(item) == false)
                return false;
        }
        return true;
    }

    private int CalcularValorTotal(List<IngredienteCantidad> conteo)
    {
        int total = 0;
        foreach (IngredienteCantidad ingrediente in conteo)
        {
            int valor = Mathf.RoundToInt(ingrediente.ingrediente.Price);
            total += valor * ingrediente.cantidadTotal;
        }
        return total;
    }

    private int CalcularValorSobrante(List<IngredienteCantidad> conteo)
    {

        int valorReceta = CalcularValorTotal(recetaMinimos);
        int valorConteo = CalcularValorTotal(conteo);
        int valorSobrante = valorConteo - valorReceta;

        return valorSobrante;
    }

}
