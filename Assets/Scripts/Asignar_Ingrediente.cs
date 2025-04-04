using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Asignar_Ingrediente : MonoBehaviour
{

    public GameObject currentResource;
    private void OnEnable()
    {
        InputManager.OnClicked += TrySetIngredient;
    }

    private void OnDisable()
    {
        InputManager.OnClicked -= TrySetIngredient;
    }


    private void TrySetIngredient()
    {
       
    }
}
