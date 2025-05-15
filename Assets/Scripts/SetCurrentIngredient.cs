using UnityEngine;

public class SetCurrentIngredient : MonoBehaviour, IInteractuable
{
    public GameObject Ingredient; // Referencia al objeto de recurso actual

    public void Interactuar()
    {

        LocalGameManager.Instance.currentIngredient = Ingredient;


        CursorManager cursorManager = FindFirstObjectByType<CursorManager>();
        GameObject cursor = cursorManager.gameObject;

        cursor.GetComponent<MeshFilter>().mesh = GetComponent<MeshFilter>().sharedMesh;

        // Cambia la escala del cursor proporcionalmente
        cursor.transform.localScale = transform.localScale * cursorManager.initialScale;
        cursor.transform.rotation = transform.rotation;

        // Copia los materiales del ingrediente al cursor
        Material[] materials = GetComponent<MeshRenderer>().sharedMaterials;
        cursor.GetComponent<MeshRenderer>().materials = materials;
    }
}

