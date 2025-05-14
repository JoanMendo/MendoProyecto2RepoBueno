using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EfectoEspecialManager : NetworkBehaviour, IEfectoManager
{
    [Header("Configuración")]
    [SerializeField] private float fuerzaEmpuje = 1f;
    [SerializeField] private float tiempoEntreEmpujes = 0.2f;

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

    // Para efectos visuales
    private ParticleSystem particulas;

    // Implementación de la interfaz IEfectoManager
    public void ConfigurarConEfecto(Efectos efecto)
    {
        if (efecto is S_Especial efectoEspecial)
        {
            efectoConfigurado = efectoEspecial;
            duracionRestante = efecto.duracion;
        }
        else
        {
            Debug.LogError("Se intentó configurar EfectoEspecialManager con un efecto que no es S_Especial");
        }
    }

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

    public void LimpiarEfecto()
    {
        if (IsServer)
        {
            // Notificar a los clientes
            LimpiarEfectoClientRpc();

            // Programar destrucción
            StartCoroutine(DestruirDespuesDeDelay(0.5f));
        }
    }

    // Singleton para evitar duplicados
    public static EfectoEspecialManager Instance { get; private set; }

    private void Awake()
    {
        // Implementación simple de singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Configurar efectos visuales
        if (particulas == null)
        {
            particulas = GetComponentInChildren<ParticleSystem>();
        }
    }

    // Método principal para iniciar el efecto
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

        // Buscar componente de efecto en el ingrediente
        if (nodoComp.currentIngredient != null)
        {
            var comp = nodoComp.currentIngredient.GetComponent<componente>();
            if (comp != null && comp.data is S_Especial)
            {
                efectoConfigurado = comp.data as S_Especial;
            }
        }

        if (efectoConfigurado == null)
        {
            Debug.LogWarning("EfectoEspecialManager: No se pudo encontrar configuración S_Especial");
        }

        // Mostrar efectos visuales
        if (particulas != null)
        {
            particulas.transform.position = nodo.transform.position + Vector3.up * 0.5f;
            particulas.Play();
        }

        Debug.Log($"Efecto especial activado desde {nodo.name} por {duracion} turnos");

        // Empujar ingredientes si estamos en el servidor
        if (IsServer)
        {
            // Iniciar el empuje inmediatamente
            EmpujarIngredientes();
        }
    }

    // Método para empujar ingredientes en la fila/columna del nodo origen
    private void EmpujarIngredientes()
    {
        if (!IsServer) return;

        Node nodoComp = nodoOrigen?.GetComponent<Node>();
        if (nodoComp == null || nodoComp.nodeMap == null) return;

        // Obtener posición del nodo origen
        Vector2Int posOrigen = nodoComp.position;

        // Para cada dirección, empujar todos los ingredientes en esa línea
        StartCoroutine(ProcesarEmpujesPorDireccion());
    }

    private IEnumerator ProcesarEmpujesPorDireccion()
    {
        Node nodoComp = nodoOrigen?.GetComponent<Node>();
        if (nodoComp == null || nodoComp.nodeMap == null) yield break;

        Vector2Int posOrigen = nodoComp.position;

        // Para cada dirección, empujar ingredientes
        foreach (Vector2Int dir in DIRECCIONES)
        {
            // Obtener todos los nodos en esa dirección que tienen ingredientes
            List<GameObject> nodosEnDireccion = ObtenerNodosEnDireccion(posOrigen, dir);

            // Filtrar solo los que tienen ingredientes y pueden moverse
            List<GameObject> nodosMovibles = nodosEnDireccion.FindAll(n => {
                Node node = n.GetComponent<Node>();
                return node != null && node.hasIngredient.Value && node.PuedeMoverse();
            });

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
    }

    // Obtener todos los nodos en una dirección específica
    private List<GameObject> ObtenerNodosEnDireccion(Vector2Int posOrigen, Vector2Int direccion)
    {
        List<GameObject> resultado = new List<GameObject>();

        Node nodoComp = nodoOrigen?.GetComponent<Node>();
        if (nodoComp == null || nodoComp.nodeMap == null) return resultado;

        // Buscar en esa dirección hasta encontrar el límite del tablero
        Vector2Int posActual = posOrigen + direccion;

        while (true)
        {
            GameObject nodoEnPos = nodoComp.nodeMap.GetNodeAtPosition(posActual);
            if (nodoEnPos == null) break; // Llegamos al límite

            resultado.Add(nodoEnPos);
            posActual += direccion;
        }

        return resultado;
    }

    // Empujar un nodo en la dirección especificada
    private void EmpujarNodo(GameObject nodo, Vector2Int direccion)
    {
        if (!IsServer) return;

        Node nodoComp = nodo.GetComponent<Node>();
        if (nodoComp == null || nodoComp.nodeMap == null) return;

        // Calcular posición destino
        Vector2Int posDestino = nodoComp.position + direccion;

        // Buscar nodo en esa posición
        GameObject nodoDestino = nodoComp.nodeMap.GetNodeAtPosition(posDestino);
        if (nodoDestino == null) return;

        Node nodoDestinoComp = nodoDestino.GetComponent<Node>();
        if (nodoDestinoComp == null) return;

        // Solo mover si el destino está vacío
        if (!nodoDestinoComp.hasIngredient.Value)
        {
            MoverIngredienteServerRpc(
                nodo.GetComponent<NetworkObject>().NetworkObjectId,
                nodoDestino.GetComponent<NetworkObject>().NetworkObjectId
            );
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoverIngredienteServerRpc(ulong origenId, ulong destinoId)
    {
        // Similar a la implementación en EfectoPicanteManager
        // Encontrar objetos por NetworkObjectId
        NetworkObject origenObj = null;
        NetworkObject destinoObj = null;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out origenObj))
        {
            Debug.LogError($"No se encontró objeto de red con ID {origenId}");
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(destinoId, out destinoObj))
        {
            Debug.LogError($"No se encontró objeto de red con ID {destinoId}");
            return;
        }

        Node nodoOrigen = origenObj.GetComponent<Node>();
        Node nodoDestino = destinoObj.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null)
        {
            Debug.LogError("Uno de los nodos no tiene componente Node");
            return;
        }

        // Si el nodo destino ya tiene ingrediente, no hacer nada
        if (nodoDestino.hasIngredient.Value)
        {
            return;
        }

        // Verificar que el nodo origen tenga ingrediente
        if (!nodoOrigen.hasIngredient.Value || nodoOrigen.currentIngredient == null)
        {
            return;
        }

        // Mover ingrediente en el servidor
        GameObject ingrediente = nodoOrigen.currentIngredient;

        // 1. Limpiar nodo origen
        nodoOrigen.hasIngredient.Value = false;
        nodoOrigen.currentIngredient = null;

        // 2. Mover el ingrediente físicamente
        ingrediente.transform.position = nodoDestino.transform.position;

        // 3. Actualizar nodo destino
        nodoDestino.hasIngredient.Value = true;
        nodoDestino.currentIngredient = ingrediente;

        // 4. Notificar a todos los clientes
        MoverIngredienteClientRpc(
            origenId,
            destinoId,
            ingrediente.GetComponent<NetworkObject>().NetworkObjectId
        );

        // 5. Reproducir efectos de sonido o visuales
        MostrarEfectoMovimientoClientRpc(
            nodoOrigen.transform.position,
            nodoDestino.transform.position
        );
    }

    [ClientRpc]
    private void MoverIngredienteClientRpc(ulong origenId, ulong destinoId, ulong ingredienteId)
    {
        // Solo ejecutar en clientes (no en servidor)
        if (IsServer) return;

        // Encontrar objetos por ID
        NetworkObject origenObj = null;
        NetworkObject destinoObj = null;
        NetworkObject ingredienteObj = null;

        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out origenObj);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(destinoId, out destinoObj);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ingredienteId, out ingredienteObj);

        if (origenObj == null || destinoObj == null || ingredienteObj == null) return;

        Node nodoOrigen = origenObj.GetComponent<Node>();
        Node nodoDestino = destinoObj.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null) return;

        // Actualizar referencias en clientes
        nodoDestino.currentIngredient = ingredienteObj.gameObject;
        nodoOrigen.currentIngredient = null;
    }

    [ClientRpc]
    private void MostrarEfectoMovimientoClientRpc(Vector3 posOrigen, Vector3 posDestino)
    {
        // Crear un efecto visual de movimiento
        GameObject efecto = new GameObject("EfectoMovimiento");
        LineRenderer linea = efecto.AddComponent<LineRenderer>();
        linea.startWidth = 0.1f;
        linea.endWidth = 0.1f;
        linea.positionCount = 2;
        linea.SetPosition(0, posOrigen + Vector3.up * 0.5f);
        linea.SetPosition(1, posDestino + Vector3.up * 0.5f);

        // Usar colores especiales
        linea.startColor = new Color(0f, 0.5f, 1f); // Azul claro
        linea.endColor = new Color(0f, 0f, 1f);     // Azul oscuro

        // Asignar material
        linea.material = new Material(Shader.Find("Sprites/Default"));

        // Destruir después de un tiempo corto
        Destroy(efecto, 0.5f);
    }

    // Método llamado cada turno
    public void ProcesarTurno()
    {
        if (!IsServer) return;

        // Reducir duración
        duracionRestante--;

        // Ejecutar empujes para este turno
        EmpujarIngredientes();

        // Si es el último turno, limpiar
        if (duracionRestante <= 0)
        {
            LimpiarEfecto();
        }
    }

    

    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        // Detener efectos visuales
        if (particulas != null)
        {
            particulas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private IEnumerator DestruirDespuesDeDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Si es el servidor, despawneamos el objeto
        if (IsServer && gameObject.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
    }
}