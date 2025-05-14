using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona el movimiento y efecto del cuchillo
/// ‡‡</summary>_PLACEHOLDER‡‡
public class CuchilloManager : NetworkBehaviour, IUtensilioManager
{
    private GameObject nodoOrigen;
    private Cuchillo cuchillo;
    private GameObject visualCuchillo;

    // Implementación de IUtensilioManager

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Configura el manager con los datos del utensilio específico
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ConfigurarConUtensilio(Utensilio utensilio)
    {
        if (utensilio is Cuchillo cuchilloUtensilio)
        {
            cuchillo = cuchilloUtensilio;
        }
        else
        {
            Debug.LogError("Se intentó configurar CuchilloManager con un utensilio que no es Cuchillo");
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Valida si los nodos seleccionados son adecuados para el cuchillo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public bool ValidarNodos(List<GameObject> nodosSeleccionados)
    {
        // El cuchillo necesita exactamente 1 nodo
        if (nodosSeleccionados == null || nodosSeleccionados.Count != 1)
            return false;

        // El nodo debe estar vacío (sin ingrediente)
        GameObject nodo = nodosSeleccionados[0];
        Node nodoComp = nodo.GetComponent<Node>();

        if (nodoComp == null)
            return false;

        // Verificamos que esté vacío
        if (nodoComp.hasIngredient.Value)
            return false;

        // Verificar que hay al menos un nodo arriba con ingrediente
        NodeMap nodeMap = FindObjectOfType<NodeMap>();
        if (nodeMap == null)
            return false;

        Vector2 posInicial = nodoComp.position;
        bool hayIngredienteArriba = false;

        // Buscar nodos hacia arriba para ver si hay algún ingrediente
        for (int y = (int)posInicial.y + 1; y < 100; y++) // Limitar búsqueda
        {
            Vector2 posArriba = new Vector2(posInicial.x, y);
            foreach (var nodoArr in nodeMap.nodesList)
            {
                Node compArr = nodoArr.GetComponent<Node>();
                if (compArr != null && compArr.position == posArriba)
                {
                    if (compArr.hasIngredient.Value)
                    {
                        hayIngredienteArriba = true;
                        break;
                    }
                }
            }

            if (hayIngredienteArriba)
                break;
        }

        return hayIngredienteArriba;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Ejecuta la acción del cuchillo
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void EjecutarAccion(List<GameObject> nodosSeleccionados)
    {
        if (!ValidarNodos(nodosSeleccionados))
            return;

        nodoOrigen = nodosSeleccionados[0];

        // Asegurar que tiene NetworkObject
        if (GetComponent<NetworkObject>() == null)
        {
            gameObject.AddComponent<NetworkObject>();
        }

        // Iniciar movimiento usando el método existente
        IniciarMovimientoCuchillo(nodoOrigen, cuchillo);
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Limpia los efectos visuales y recursos
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void LimpiarEfecto()
    {
        if (visualCuchillo != null)
        {
            Destroy(visualCuchillo);
            visualCuchillo = null;
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicia el movimiento del cuchillo desde un nodo origen
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void IniciarMovimientoCuchillo(GameObject origen, Cuchillo cuchilloSO)
    {
        nodoOrigen = origen;
        cuchillo = cuchilloSO;

        // Spawnear objeto de red si no está spawneado ya
        if (!GetComponent<NetworkObject>().IsSpawned)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Solicitar movimiento al servidor
        MoverCuchilloServerRpc(origen.GetComponent<NetworkObject>().NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoverCuchilloServerRpc(ulong origenId)
    {
        // Obtener referencia al nodo origen de forma segura
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out NetworkObject origenNetObj))
        {
            Debug.LogError("CuchilloManager: No se pudo encontrar el nodo origen por ID");
            Destroy(gameObject);
            return;
        }

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

        // Obtener referencia al nodo origen de forma segura
        NetworkObject origenNetObj = null;
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out origenNetObj);

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

            // Mover visual del cuchillo al siguiente nodo de forma segura
            ulong nodoId = nodoSiguiente.GetComponent<NetworkObject>().NetworkObjectId;
            MoverVisualClientRpc(nodoId);

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
        // Obtener nodo por ID de forma segura
        NetworkObject nodoNetObj = null;
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodoId, out nodoNetObj);

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
            visualCuchillo = null;
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
        LimpiarEfecto();
    }
}