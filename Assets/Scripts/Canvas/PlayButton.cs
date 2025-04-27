using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkSceneLoader_Distributed : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Gameplay";
    [SerializeField] private Button loadSceneButton;

    private void Awake()
    {
        if (loadSceneButton != null)
        {
            loadSceneButton.onClick.AddListener(OnLoadSceneClicked);
        }
        else
        {
            Debug.LogError("Botón de escena no asignado.");
        }
    }

    private void OnLoadSceneClicked()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("No existe NetworkManager.");
            return;
        }

        // Si somos el host, cargamos la escena directamente
        if (NetworkManager.Singleton.IsHost)
        {
            LoadSceneForEveryone();
        }
        // Si somos un cliente, solicitamos al servidor que la cargue
        else if (NetworkManager.Singleton.IsClient)
        {
            RequestSceneLoadServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSceneLoadServerRpc(ServerRpcParams rpcParams = default)
    {
        LoadSceneForEveryone();
    }

    private void LoadSceneForEveryone()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"Cargando escena '{sceneToLoad}' para todos los jugadores.");
            NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
