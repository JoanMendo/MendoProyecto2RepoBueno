using UnityEngine;

public class SetCurrentIngredient : MonoBehaviour, IInteractuable
{

    public GameObject Ingredient; // Referencia al objeto de recurso actual
    public IngredientHolder ingredientData;


    private void Awake()
    {
       
            // Verificar primero en este GameObject
            ingredientData = GetComponent<IngredientHolder>();

            // Si no est� aqu�, buscar en el GameObject del ingrediente (si est� asignado)
            if (ingredientData == null && Ingredient != null)
            {
                ingredientData = Ingredient.GetComponent<IngredientHolder>();
            }

            if (ingredientData == null)
            {
                Debug.LogWarning($"No se encontr� IngredientHolder para {gameObject.name}. Asigna uno manualmente o a�ade el componente.");
            }
        
    }

    public void Interactuar()
    {
        LocalGameManager.Instance.currentIngredient = Ingredient; // Asigna el objeto de recurso actual al GameManager
        LocalGameManager.Instance.currentIngredientData = ingredientData.datos;
        GameObject cursor = FindFirstObjectByType<CursorManager>().gameObject; // Encuentra el objeto del cursor
       cursor.GetComponent<MeshFilter>().mesh = Ingredient.GetComponent<MeshFilter>().sharedMesh; // Cambia el mesh del cursor al del ingrediente
    }
}
