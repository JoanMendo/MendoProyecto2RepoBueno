using UnityEngine;
using Unity.Netcode;
using TMPro;

public abstract class AbstractIngredient : NetworkBehaviour
{
    public int initialValue;
    public int actualValue;
    public Node node; // Nodo al que pertenece el ingrediente
    public abstract void Efecto();



    public float fontSize = 25.0f;

    protected GameObject textObj;
    protected TextMeshPro textMeshPro;

    public void SetFloatingText()
    {
        // Crear objeto vacío para el texto
        textObj = new GameObject("FloatingTextTMP");

        // Hacer hijo del objeto actual
        textObj.transform.SetParent(transform, true);

        // Posicionar encima del objeto
        textObj.transform.localPosition = new Vector3(0, 0, 0);


        // Añadir componente TextMeshPro
        textMeshPro = textObj.AddComponent<TextMeshPro>();
        textMeshPro.text = actualValue.ToString();
        textMeshPro.fontSize = fontSize;
        textMeshPro.color = Color.red;
        textMeshPro.fontStyle = FontStyles.Bold;
        textMeshPro.alignment = TextAlignmentOptions.Center;
        textMeshPro.rectTransform.pivot = new Vector2(0.5f, 0);

        // Para evitar que se corte el texto
        textMeshPro.textWrappingMode = TextWrappingModes.NoWrap;
        textMeshPro.isOverlay = true;

        textObj.AddComponent<LookAtCameraTMP>();


    }

}
