using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Utensilio que recorre nodos hacia arriba y destruye el primer ingrediente que encuentra
/// ‡‡</summary>_PLACEHOLDER‡‡
[CreateAssetMenu(fileName = "Cuchillo", menuName = "CookingGame/Resources/Utensilios/Cuchillo")]
public class Cuchillo : Utensilio
{
    [Tooltip("Velocidad de movimiento del cuchillo")]
    public float velocidadMovimiento = 0.2f;

    [Tooltip("Prefab para efecto visual al cortar")]
    public GameObject prefabEfectoCorte;

    protected override void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Crear gestor para el movimiento del cuchillo
        GameObject gestorObj = new GameObject("CuchilloManager");
        CuchilloManager gestor = gestorObj.AddComponent<CuchilloManager>();

        // Iniciar movimiento
        gestor.IniciarMovimientoCuchillo(nodoOrigen, this);
    }
}

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona el movimiento y efecto del cuchillo
/// ‡‡</summary>_PLACEHOLDER‡‡
public class CuchilloManager : NetworkBehaviour
{
    private GameObject nodoOrigen;
    private Cuchillo cuchillo;
    private GameObject visualCuchillo;

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el movimiento del cuchillo desde un nodo origen
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarMovimientoCuchillo(GameObject origen, Cuchillo cuchilloSO)
    {
        nodoOrigen = origen;
        cuchillo = cuchilloSO;

        // Spawnear objeto de red
        GetComponent<NetworkObject>().Spawn();

        // Solicitar movimiento al servidor
        MoverCuchilloServerRpc(origen.GetComponent<NetworkObject>().NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoverCuchilloServerRpc(ulong origenId)
    {
        // Obtener referencia al nodo origen
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        if (origenNetObj == null) return;

        nodoOrigen = origenNetObj.gameObject;

        // Verificar que el nodo origen está vacío
        Node nodoOrigenComp = nodoOrigen.GetComponent<Node>();
        if (nodoOrigenComp == null || nodoOrigenComp.hasIngredient.Value)
        {
            Debug.Log("El nodo origen debe estar vacío para usar el cuchillo");
            Destroy(gameObject);
            return;
        }

        // Iniciar movimiento en todos los clientes
        IniciarMovimientoClientRpc(origenId);

        // Iniciar coroutine para movimiento en el servidor
        StartCoroutine(MoverCuchilloHaciaArriba());
    }

    [ClientRpc]
    private void IniciarMovimientoClientRpc(ulong origenId)
    {
        // Si estamos en el servidor, no hacer nada (ya está gestionado)
        if (IsServer) return;

        // Obtener referencia al nodo origen
        NetworkObject origenNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[origenId];
        if (origenNetObj == null) return;

        nodoOrigen = origenNetObj.gameObject;

        // Visualizar movimiento inicial
        MostrarVisualCuchillo(nodoOrigen.transform.position);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Crea el visual del cuchillo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private void MostrarVisualCuchillo(Vector3 posicion)
    {
        if (cuchillo.prefab3D != null)
        {
            visualCuchillo = Instantiate(cuchillo.prefab3D, posicion, Quaternion.identity);
        }
        else
        {
            // Crear un placeholder si no hay prefab
            visualCuchillo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualCuchillo.transform.localScale = new Vector3(0.3f, 0.8f, 0.1f);
            visualCuchillo.transform.position = posicion;
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Coroutine que gestiona el movimiento del cuchillo hacia arriba
    /// ‡‡</summary>_PLACEHOLDER‡‡
    private IEnumerator MoverCuchilloHaciaArriba()
    {
        // Obtener NodeMap
        NodeMap nodeMap = FindObjectOfType<NodeMap>();
        if (nodeMap == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // Obtener posición inicial
        Node nodoActualComp = nodoOrigen.GetComponent<Node>();
        if (nodoActualComp == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Vector2 posActual = nodoActualComp.position;
        GameObject nodoActual = nodoOrigen;

        // Dirección hacia arriba
        Vector2 direccion = Vector2.up;

        while (true)
        {
            // Calcular siguiente posición
            posActual += direccion;

            // Buscar nodo en esa posición
            GameObject nodoSiguiente = null;
            foreach (var nodo in nodeMap.nodesList)
            {
                Node componente = nodo.GetComponent<Node>();
                if (componente != null && componente.position == posActual)
                {
                    nodoSiguiente = nodo;
                    break;
                }
            }

            // Si no hay más nodos arriba, terminar
            if (nodoSiguiente == null)
            {
                // Notificar fin del movimiento
                FinMovimientoClientRpc(Vector3.zero, false);
                Destroy(gameObject, 0.2f);
                yield break;
            }

            // Mover visual del cuchillo al siguiente nodo
            MoverVisualClientRpc(nodoSiguiente.GetComponent<NetworkObject>().NetworkObjectId);

            // Esperar para dar sensación de movimiento
            yield return new WaitForSeconds(cuchillo.velocidadMovimiento);

            // Verificar si hay ingrediente
            Node nodoSiguienteComp = nodoSiguiente.GetComponent<Node>();
            if (nodoSiguienteComp.hasIngredient.Value)
            {
                // Destruir ingrediente
                nodoSiguienteComp.ClearNodeIngredient();

                // Notificar efecto de corte
                Vector3 posCorte = nodoSiguiente.transform.position;
                FinMovimientoClientRpc(posCorte, true);

                Debug.Log("Ingrediente destruido con cuchillo");
                Destroy(gameObject, 0.5f);
                yield break;
            }

            // Actualizar nodo actual
            nodoActual = nodoSiguiente;
        }
    }

    [ClientRpc]
    private void MoverVisualClientRpc(ulong nodoId)
    {
        // Obtener nodo por ID
        NetworkObject nodoNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[nodoId];
        if (nodoNetObj == null) return;

        // Mover visual
        if (visualCuchillo != null)
        {
            visualCuchillo.transform.position = nodoNetObj.transform.position;
        }
    }

    [ClientRpc]
    private void FinMovimientoClientRpc(Vector3 posicionCorte, bool mostrarEfecto)
    {
        // Destruir visual del cuchillo
        if (visualCuchillo != null)
        {
            Destroy(visualCuchillo);
        }

        // Mostrar efecto de corte si corresponde
        if (mostrarEfecto && cuchillo.prefabEfectoCorte != null)
        {
            Instantiate(cuchillo.prefabEfectoCorte, posicionCorte, Quaternion.identity);
        }
    }

    private void OnDestroy()
    {
        // Asegurar limpieza del visual
        if (visualCuchillo != null)
        {
            Destroy(visualCuchillo);
        }
    }
}
