using TMPro;
using UnityEngine;


public class tomateScript : AbstractIngredient
{
    private void Start()
    {
        initialValue = 3;
        actualValue = 3;
        SetFloatingText(); 
    }
    public override void Efecto()
    {
        if (initialValue != actualValue)
        {
            actualValue = initialValue; // Resetea el valor al inicial
        }
        textObj.GetComponent<TextMeshPro>().text = actualValue.ToString();
    }
}
  

