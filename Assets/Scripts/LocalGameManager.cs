using UnityEngine;

public class LocalGameManager : MonoBehaviour
{
    public GameObject currentIngredient;
    public int ingredientCount = 0;
    public int utensilEffectsCount = 0;

    public int maxUtensilEffects = 2;
    public int maxIngredients = 3;

    public static LocalGameManager Instance { get; private set; }

    public void Awake()
    {
        Instance = this;
    }
}
