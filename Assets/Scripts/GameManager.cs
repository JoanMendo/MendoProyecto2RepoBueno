using UnityEngine;

public class GameManager : MonoBehaviour
{
   public GameObject currentIngredient;
    public static GameManager Instance { get; private set; }

    public void Awake()
    {
        Instance = this;
    }
}
