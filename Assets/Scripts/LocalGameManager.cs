using UnityEngine;

public class LocalGameManager : MonoBehaviour
{
    public GameObject currentIngredient;
    public ResourcesSO currentIngredientData; // Datos
    public float actualmoney;
    public static LocalGameManager Instance { get; private set; }

    public void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Asignar dinero inicial (podr��as ajustar esta cantidad)
        actualmoney = 100f;
        Debug.Log($"Dinero inicial establecido: {actualmoney}");

        /* Si quieres sincronizar con Economia
        Economia economia = FindFirstObjectByType<Economia>();
        if (economia != null && economia.IsOwner)
        {
            economia.more_money(actualmoney);
        }*/
    }
}
