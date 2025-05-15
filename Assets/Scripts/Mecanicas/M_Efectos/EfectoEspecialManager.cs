using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

/// <summary>
/// Gestor para el efecto S_Especial que mueve ingredientes en las cuatro direcciones.
/// </summary>
public class EfectoEspecialManager : NetworkBehaviour, IEfectoManager
{
    [Header("Configuración")]
    [SerializeField] private float fuerzaEmpuje = 1f;
    [SerializeField] private float tiempoEntreEmpujes = 0.2f;
    [SerializeField] private bool mostrarDebug = true;

    // Direcciones posibles para empujar
    private static readonly Vector2Int[] DIRECCIONES = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Arriba
        new Vector2Int(1, 0),   // Derecha
        new Vector2Int(0, -1),  // Abajo
        new Vector2Int(-1, 0)   // Izquierda
    };

    // Referencias
    private GameObject nodoOrigen;
    private S_Especial efectoConfigurado;
    private int duracionRestante;
    private NodeMap mapaDeNodos;

    // Para efectos visuales
    private ParticleSystem particulas;

    // Estado de ejecución
    private NetworkVariable<bool> efectoActivo = new NetworkVariable<bool>(false);
    private Coroutine rutinaEmpujes;

    /// <summary>
    /// Configura el manager con los datos del efecto
    /// </summary>
    public void ConfigurarConEfecto(Efectos efecto)
    {
        if (efecto is S_Especial efectoEspecial)
        {
            efectoConfigurado = efectoEspecial;
            duracionRestante = efecto.duracion;

            if (mostrarDebug)
            {
                Debug.Log($"EfectoEspecialManager configurado con: {efecto.name}, duración: {duracionRestante}");
            }
        }
        else
        {
            Debug.LogError("Se intentó configurar EfectoEspecialManager con un efecto que no es S_Especial");
        }
    }

    /// <summary>
    /// Valida si los nodos seleccionados son adecuados para este efecto
    /// </summary>
    public bool ValidarNodos(List<GameObject> nodosSeleccionados)
    {
        // S_Especial necesita exactamente 1 nodo
        if (nodosSeleccionados == null || nodosSeleccionados.Count != 1)
            return false;

        // El nodo debe tener componente Node
        Node nodo = nodosSeleccionados[0].GetComponent<Node>();
        if (nodo == null)
            return false;

        // Aquí puedes añadir validaciones específicas
        // Por ejemplo, verificar que hay suficiente espacio en 4 direcciones
        return true;
    }

    /// <summary>
    /// Ejecuta la acción del efecto sobre los nodos seleccionados
    /// </summary>
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

        // Iniciar el efecto
        IniciarEfecto(nodoOrigen, duracionRestante);
    }

    /// <summary>
    /// Inicia el efecto especial en el nodo seleccionado
    /// </summary>
    public void IniciarEfecto(GameObject nodo, int duracion)
    {
        // Validaciones
        if (nodo == null)
        {
            Debug.LogError("EfectoEspecialManager: nodo es null");
            return;
        }

        // Guardar referencias
        nodoOrigen = nodo;
        duracionRestante = duracion;

        // Obtener nodo y NodeMap
        Node nodoComp = nodo.GetComponent<Node>();
        if (nodoComp == null)
        {
            Debug.LogError("EfectoEspecialManager: nodo no tiene componente Node");
            return;
        }

        // Guardar referencia al mapa de nodos
        mapaDeNodos = nodoComp.nodeMap;
        if (mapaDeNodos == null)
        {
            Debug.LogError("EfectoEspecialManager: No se puede obtener referencia al NodeMap");
            return;
        }

        // Buscar componente de efecto en el ingrediente
        if (nodoComp.currentIngredient != null)
        {
            var comp = nodoComp.currentIngredient.GetComponent<componente>();
            if (comp != null && comp.data is S_Especial)
            {
                efectoConfigurado = comp.data as S_Especial;
            }
        }

        // Asegurar que estamos en la red
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        // Configurar efectos visuales
        ConfigurarEfectosVisuales(nodo);

        // Activar el efecto
        efectoActivo.Value = true;

        if (mostrarDebug)
        {
            Debug.Log($"Efecto especial activado desde {nodo.name} por {duracion} turnos");
        }

        // Empujar ingredientes si estamos en el servidor
        if (IsServer)
        {
            // Iniciar el empuje inmediatamente
            IniciarEmpujesServerRpc();
        }
    }

    /// <summary>
    /// Configura los efectos visuales del efecto especial
    /// </summary>
    private void ConfigurarEfectosVisuales(GameObject nodo)
    {
        // Crear sistema de partículas si no existe
        if (particulas == null)
        {
            GameObject efectoVisual = new GameObject("EfectoEspecialVFX");
            efectoVisual.transform.SetParent(transform);
            particulas = efectoVisual.AddComponent<ParticleSystem>();

            // Configuración básica de partículas
            var main = particulas.main;
            main.startColor = Color.green;
            main.startSize = 0.1f;
            main.startLifetime = 2f;
            main.startSpeed = 1f;

            // Posicionar el sistema de partículas
            efectoVisual.transform.position = nodo.transform.position + Vector3.up * 0.5f;
        }

        // Activar partículas
        if (particulas != null)
        {
            particulas.Play();
        }
    }

    /// <summary>
    /// Server RPC para iniciar el proceso de empujes
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void IniciarEmpujesServerRpc()
    {
        if (mostrarDebug)
        {
            Debug.Log("Servidor: Iniciando proceso de empujes en cuatro direcciones");
        }

        // Iniciar coroutine para procesar empujes secuenciales
        if (rutinaEmpujes == null)
        {
            rutinaEmpujes = StartCoroutine(ProcesarEmpujesPorDireccion());
        }
    }

    /// <summary>
    /// Coroutine que procesa empujes en cada dirección
    /// </summary>
    private IEnumerator ProcesarEmpujesPorDireccion()
    {
        if (mostrarDebug)
        {
            Debug.Log("Iniciando coroutine de empujes por dirección");
        }

        Node nodoComp = nodoOrigen?.GetComponent<Node>();
        if (nodoComp == null || mapaDeNodos == null)
        {
            if (mostrarDebug)
            {
                Debug.LogError("Componentes necesarios no encontrados para empujes");
            }
            yield break;
        }

        Vector2Int posOrigen = nodoComp.position;

        // Para cada dirección, empujar ingredientes
        foreach (Vector2Int dir in DIRECCIONES)
        {
            if (mostrarDebug)
            {
                Debug.Log($"Procesando empujes en dirección: {dir}");
            }

            // Obtener todos los nodos en esa dirección que tienen ingredientes
            List<GameObject> nodosEnDireccion = ObtenerNodosEnDireccion(posOrigen, dir);

            // Filtrar solo los que tienen ingredientes y pueden moverse
            List<GameObject> nodosMovibles = nodosEnDireccion.FindAll(n => {
                Node node = n.GetComponent<Node>();
                return node != null && node.hasIngredient.Value && node.PuedeMoverse();
            });

            if (mostrarDebug)
            {
                Debug.Log($"Encontrados {nodosMovibles.Count} nodos movibles en dirección {dir}");
            }

            // Si hay nodos que empujar, realizar empuje
            if (nodosMovibles.Count > 0)
            {
                // Procesar nodos desde el más lejano al más cercano al origen
                // para evitar colisiones durante el empuje
                nodosMovibles.Sort((a, b) => {
                    Node nodeA = a.GetComponent<Node>();
                    Node nodeB = b.GetComponent<Node>();

                    int distA = Mathf.Abs(nodeA.position.x - posOrigen.x) +
                                Mathf.Abs(nodeA.position.y - posOrigen.y);

                    int distB = Mathf.Abs(nodeB.position.x - posOrigen.x) +
                                Mathf.Abs(nodeB.position.y - posOrigen.y);

                    // Orden descendente (más lejano primero)
                    return distB.CompareTo(distA);
                });

                // Empujar cada nodo
                foreach (var nodo in nodosMovibles)
                {
                    EmpujarNodo(nodo, dir);
                    yield return new WaitForSeconds(tiempoEntreEmpujes);
                }
            }

            // Esperar un poco antes de la siguiente dirección
            yield return new WaitForSeconds(0.1f);
        }

        // Limpiar la referencia a la coroutine
        rutinaEmpujes = null;

        if (mostrarDebug)
        {
            Debug.Log("Completado el proceso de empujes en todas direcciones");
        }
    }

    /// <summary>
    /// Obtiene todos los nodos en una dirección específica desde el origen
    /// </summary>
    private List<GameObject> ObtenerNodosEnDireccion(Vector2Int posOrigen, Vector2Int direccion)
    {
        List<GameObject> resultado = new List<GameObject>();

        if (mapaDeNodos == null || mapaDeNodos.nodesList == null)
        {
            if (mostrarDebug)
            {
                Debug.LogError("No se puede obtener la lista de nodos del mapa");
            }
            return resultado;
        }

        // Buscar en esa dirección hasta encontrar el límite del tablero
        Vector2Int posActual = posOrigen + direccion;

        while (true)
        {
            GameObject nodoEnPos = mapaDeNodos.GetNodeAtPosition(posActual);
            if (nodoEnPos == null) break; // Llegamos al límite

            resultado.Add(nodoEnPos);
            posActual += direccion;
        }

        return resultado;
    }

    /// <summary>
    /// Empuja un nodo en la dirección especificada
    /// </summary>
    private void EmpujarNodo(GameObject nodo, Vector2Int direccion)
    {
        if (!IsServer || nodo == null) return;

        Node nodoComp = nodo.GetComponent<Node>();
        if (nodoComp == null || mapaDeNodos == null) return;

        // Calcular posición destino
        Vector2Int posDestino = nodoComp.position + direccion;

        // Buscar nodo en esa posición
        GameObject nodoDestino = mapaDeNodos.GetNodeAtPosition(posDestino);
        if (nodoDestino == null) return;

        Node nodoDestinoComp = nodoDestino.GetComponent<Node>();
        if (nodoDestinoComp == null) return;

        // Solo mover si el destino está vacío
        if (!nodoDestinoComp.hasIngredient.Value)
        {
            // Verificar que ambos nodos tienen NetworkObject
            NetworkObject origenNetObj = nodo.GetComponent<NetworkObject>();
            NetworkObject destinoNetObj = nodoDestino.GetComponent<NetworkObject>();

            if (origenNetObj != null && destinoNetObj != null &&
                origenNetObj.IsSpawned && destinoNetObj.IsSpawned)
            {
                MoverIngredienteServerRpc(
                    origenNetObj.NetworkObjectId,
                    destinoNetObj.NetworkObjectId
                );
            }
            else if (mostrarDebug)
            {
                Debug.LogWarning("No se pudo mover: nodos sin NetworkObject válido");
            }
        }
    }

    /// <summary>
    /// Server RPC para mover un ingrediente entre nodos
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void MoverIngredienteServerRpc(ulong origenId, ulong destinoId)
    {
        // Verificar que el NetworkManager existe
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
        {
            Debug.LogError("NetworkManager o SpawnManager no disponibles");
            return;
        }

        // Encontrar objetos por NetworkObjectId
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out NetworkObject origenObj) ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(destinoId, out NetworkObject destinoObj))
        {
            Debug.LogError($"No se encontraron objetos con IDs {origenId} o {destinoId}");
            return;
        }

        Node nodoOrigen = origenObj.GetComponent<Node>();
        Node nodoDestino = destinoObj.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null)
        {
            Debug.LogError("Uno de los nodos no tiene componente Node");
            return;
        }

        // Verificar condiciones para el movimiento
        if (!nodoOrigen.hasIngredient.Value || nodoDestino.hasIngredient.Value)
        {
            if (mostrarDebug)
            {
                Debug.LogWarning("Condiciones inválidas para mover: origen sin ingrediente o destino ocupado");
            }
            return;
        }

        // Obtener referencia al ingrediente
        GameObject ingrediente = nodoOrigen.currentIngredient;
        if (ingrediente == null)
        {
            Debug.LogError("Nodo origen no tiene ingrediente válido");
            return;
        }

        // Verificar que el ingrediente tiene NetworkObject
        NetworkObject ingredienteNetObj = ingrediente.GetComponent<NetworkObject>();
        if (ingredienteNetObj == null || !ingredienteNetObj.IsSpawned)
        {
            Debug.LogError("El ingrediente no tiene NetworkObject válido");
            return;
        }

        // 1. Obtener datos del ingrediente para recreación
        componente comp = ingrediente.GetComponent<componente>();
        if (comp == null || comp.data == null || comp.data.prefab3D == null)
        {
            Debug.LogError("No se pueden obtener datos del ingrediente para recrearlo");
            return;
        }

        GameObject prefabIngrediente = comp.data.prefab3D;

        // 2. Limpiar nodo origen
        nodoOrigen.ClearNodeIngredient();

        // 3. Colocar ingrediente en destino
        nodoDestino.SetNodeIngredient(prefabIngrediente);

        // 4. Notificar a todos los clientes
        MoverIngredienteClientRpc(
            origenId,
            destinoId,
            ingredienteNetObj.NetworkObjectId
        );

        // 5. Mostrar efecto visual
        MostrarEfectoMovimientoClientRpc(
            nodoOrigen.transform.position,
            nodoDestino.transform.position
        );

        if (mostrarDebug)
        {
            Debug.Log($"Ingrediente movido exitosamente de {nodoOrigen.position} a {nodoDestino.position}");
        }
    }

    /// <summary>
    /// Client RPC para sincronizar el movimiento en todos los clientes
    /// </summary>
    [ClientRpc]
    private void MoverIngredienteClientRpc(ulong origenId, ulong destinoId, ulong ingredienteId)
    {
        // Solo ejecutar en clientes (no en servidor)
        if (IsServer) return;

        if (mostrarDebug)
        {
            Debug.Log($"Cliente: Sincronizando movimiento de ingrediente {ingredienteId}");
        }

        // Encontrar objetos por ID
        NetworkObject origenObj = null;
        NetworkObject destinoObj = null;
        NetworkObject ingredienteObj = null;

        bool origenEncontrado = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out origenObj);
        bool destinoEncontrado = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(destinoId, out destinoObj);
        bool ingredienteEncontrado = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ingredienteId, out ingredienteObj);

        if (!origenEncontrado || !destinoEncontrado)
        {
            Debug.LogWarning($"Cliente: No se pudieron encontrar los nodos. Origen: {origenEncontrado}, Destino: {destinoEncontrado}");
            return;
        }

        Node nodoOrigen = origenObj.GetComponent<Node>();
        Node nodoDestino = destinoObj.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null)
        {
            Debug.LogWarning("Cliente: Los nodos no tienen componente Node");
            return;
        }

        // Si el ingrediente aún existe, actualizamos la referencia
        if (ingredienteEncontrado && ingredienteObj != null)
        {
            nodoDestino.currentIngredient = ingredienteObj.gameObject;
            nodoOrigen.currentIngredient = null;
        }
        else
        {
            // Si no encontramos el ingrediente, es porque ya fue recreado por SetNodeIngredient
            if (mostrarDebug)
            {
                Debug.Log("Cliente: Ingrediente ya fue recreado por el servidor");
            }
        }
    }

    /// <summary>
    /// Client RPC para mostrar efectos visuales del movimiento
    /// </summary>
    [ClientRpc]
    private void MostrarEfectoMovimientoClientRpc(Vector3 posOrigen, Vector3 posDestino)
    {
        // Crear un efecto visual de movimiento
        GameObject efecto = new GameObject("EfectoMovimientoEspecial");
        LineRenderer linea = efecto.AddComponent<LineRenderer>();
        linea.startWidth = 0.1f;
        linea.endWidth = 0.1f;
        linea.positionCount = 2;
        linea.SetPosition(0, posOrigen + Vector3.up * 0.5f);
        linea.SetPosition(1, posDestino + Vector3.up * 0.5f);

        // Usar colores azules para el efecto especial
        linea.startColor = new Color(0f, 0.5f, 1f); // Azul claro
        linea.endColor = new Color(0f, 0f, 1f);     // Azul oscuro

        // Asignar material
        linea.material = new Material(Shader.Find("Sprites/Default"));

        // Destruir después de un tiempo corto
        Destroy(efecto, 0.5f);
    }

    /// <summary>
    /// Método llamado cada turno para ejecutar el efecto
    /// </summary>
    public void ProcesarTurno()
    {
        if (!IsServer) return;

        // Reducir duración
        duracionRestante--;

        if (mostrarDebug)
        {
            Debug.Log($"Procesando turno de efecto especial. Turnos restantes: {duracionRestante}");
        }

        // Ejecutar empujes para este turno
        IniciarEmpujesServerRpc();

        // Si es el último turno, finalizar efecto
        if (duracionRestante <= 0)
        {
            if (mostrarDebug)
            {
                Debug.Log("Último turno completado. Finalizando efecto especial.");
            }
            LimpiarEfecto();
        }
    }

    /// <summary>
    /// Limpia el efecto y los recursos asociados
    /// </summary>
    public void LimpiarEfecto()
    {
        if (mostrarDebug)
        {
            Debug.Log("EfectoEspecialManager: LimpiarEfecto llamado");
        }

        // Detener coroutine si está en ejecución
        if (rutinaEmpujes != null)
        {
            StopCoroutine(rutinaEmpujes);
            rutinaEmpujes = null;
        }

        // Desactivar estado de ejecución
        efectoActivo.Value = false;

        if (IsServer)
        {
            // Notificar a los clientes
            LimpiarEfectoClientRpc();

            // Programar destrucción
            StartCoroutine(DestroyAfterDelay(0.5f));
        }
    }

    /// <summary>
    /// Client RPC para limpiar efectos en clientes
    /// </summary>
    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        if (mostrarDebug)
        {
            Debug.Log("Cliente: Limpiando efecto especial");
        }

        // Detener efectos visuales
        if (particulas != null)
        {
            particulas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    /// <summary>
    /// Destruye el manager después de un retraso
    /// </summary>
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
        else if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Asegurar limpieza adecuada
        if (rutinaEmpujes != null)
        {
            StopCoroutine(rutinaEmpujes);
        }

        // Detener particulas
        if (particulas != null)
        {
            particulas.Stop(true);
            Destroy(particulas.gameObject);
        }
    }
}