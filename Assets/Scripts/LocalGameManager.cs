using UnityEngine;

public class LocalGameManager : MonoBehaviour
{
    public GameObject currentIngredient;
    public int ingredientCount = 0;
    public int utensilEffectsCount = 0;

    public int maxUtensilEffects = 3;
    public int maxIngredients = 2;

    public static LocalGameManager Instance { get; private set; }

    public void Awake()
    {
        Instance = this;
    }
}
