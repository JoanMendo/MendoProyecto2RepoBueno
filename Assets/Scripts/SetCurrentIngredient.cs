using UnityEngine;

public class SetCurrentIngredient : MonoBehaviour, IInteractuable
{

    public GameObject Ingredient; // Referencia al objeto de recurso actual

    public void Interactuar()
    {
       GameManager.Instance.currentIngredient = Ingredient; // Asigna el objeto de recurso actual al GameManager
       GameObject cursor = FindFirstObjectByType<CursorManager>().gameObject; // Encuentra el objeto del cursor
       cursor.GetComponent<MeshFilter>().mesh = Ingredient.GetComponent<MeshFilter>().sharedMesh; // Cambia el mesh del cursor al del ingrediente
    }
}
