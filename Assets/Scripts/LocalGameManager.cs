using UnityEngine;

public class LocalGameManager : MonoBehaviour
{
    public GameObject currentIngredient;
    public float actualmoney;
    public static LocalGameManager Instance { get; private set; }

    public void Awake()
    {
        Instance = this;
    }
}
