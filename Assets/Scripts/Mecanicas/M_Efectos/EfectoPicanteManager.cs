using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EfectoPicanteManager : NetworkBehaviour, IEfectoManager
{
    [SerializeField] private float delayEntreMovimientos = 0.5f;
    [SerializeField] private int movimientosPorTurno = 1;

    // Referencias
    private GameObject nodoOrigen;
    private S_Picante efectoConfigurado;
    private int duracionRestante;

    // NUEVO: Lista de nodos afectados por el efecto
    private List<GameObject> nodosAfectados = new List<GameObject>();
    private NetworkVariable<bool> efectoEnNodo = new NetworkVariable<bool>(true);

    // Estado de ejecución
    private bool efectoActivo = false;
    private Coroutine rutinaDeProcesamiento;

    // Para efectos visuales
    private ParticleSystem particulas;
    private List<GameObject> efectosVisualesNodos = new List<GameObject>();

    public void ConfigurarConIngrediente(IngredientesSO ingrediente)
    {
        Debug.LogError("EfectoPicanteManager debe ser configurado con un efecto, no con un ingrediente");
    }

    public void ConfigurarConEfecto(Efectos efecto)
    {
        if (efecto is S_Picante efectoPicante)
        {
            efectoConfigurado = efectoPicante;
            duracionRestante = efecto.duracion;
        }
        else
        {
            Debug.LogError("Se intentó configurar EfectoPicanteManager con un efecto que no es S_Picante");
        }
    }

    public bool ValidarNodos(List<GameObject> nodosSeleccionados)
    {
        // S_Picante necesita exactamente 1 nodo
        if (nodosSeleccionados == null || nodosSeleccionados.Count != 1)
            return false;

        // El nodo debe tener componente Node
        Node nodo = nodosSeleccionados[0].GetComponent<Node>();
        if (nodo == null)
            return false;

        // Validaciones adicionales específicas para S_Picante
        // Por ejemplo, verificar que hay ingredientes que pueden moverse en el tablero
        return true;
    }

    public void EjecutarAccion(List<GameObject> nodosSeleccionados)
    {
        if (!ValidarNodos(nodosSeleccionados))
            return;

        nodoOrigen = nodosSeleccionados[0];

        // NUEVO: Guardar los nodos afectados
        nodosAfectados = new List<GameObject>(nodosSeleccionados);

        // Asegurar que tiene NetworkObject
        if (GetComponent<NetworkObject>() == null)
        {
            gameObject.AddComponent<NetworkObject>();
        }

        // Iniciar el efecto
        IniciarEfecto(nodoOrigen, efectoConfigurado, duracionRestante);
    }

    public void LimpiarEfecto()
    {
        if (IsServer)
        {
            // Detener coroutine si está en ejecución
            if (rutinaDeProcesamiento != null)
            {
                StopCoroutine(rutinaDeProcesamiento);
                efectoActivo = false;
            }

            // Notificar a los clientes
            LimpiarEfectoClientRpc();

            // Programar destrucción después de un tiempo
            StartCoroutine(DestroyAfterDelay(0.5f));
        }
    }

    // Método principal para iniciar el efecto
    public void IniciarEfecto(GameObject nodo, S_Picante efecto, int duracion)
    {
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Spawn();
        }

        this.nodoOrigen = nodo;
        this.efectoConfigurado = efecto;
        this.duracionRestante = duracion;

        // NUEVO: Crear efectos visuales para los nodos afectados
        if (IsServer)
        {
            CrearEfectosVisualesNodosServerRpc();
        }

        // Mostrar efectos visuales
        if (particulas != null)
        {
            particulas.transform.position = nodo.transform.position + Vector3.up * 0.5f;
            particulas.Play();
        }

        Debug.Log($"Efecto picante activado desde {nodo.name} afectando a 1 nodos por {duracion} turnos");

        // Iniciar movimientos aleatorios si estamos en el servidor
        if (IsServer)
        {
            Debug.Log("Iniciando efecto picante");
            // Iniciar el proceso de movimientos aleatorios
            MoverAleatoriamente();
        }
    }

    // NUEVO: Método para crear efectos visuales en los nodos
    [ServerRpc(RequireOwnership = false)]
    private void CrearEfectosVisualesNodosServerRpc()
    {
        foreach (var nodo in nodosAfectados)
        {
            if (nodo != null)
            {
                CrearEfectoVisualNodoClientRpc(nodo.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }
    }

    [ClientRpc]
    private void CrearEfectoVisualNodoClientRpc(ulong nodoId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodoId, out NetworkObject nodoNetObj))
            return;

        GameObject nodo = nodoNetObj.gameObject;

        // Crear un efecto visual simple para el nodo
        GameObject efectoVisual = new GameObject("EfectoPicanteNodo");
        efectoVisual.transform.position = nodo.transform.position + Vector3.up * 0.2f;

        // Añadir un sistema de partículas
        ParticleSystem ps = efectoVisual.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startSize = 0.2f;
        main.startColor = Color.red;
        main.startLifetime = 2f;

        // Guardar referencia para poder destruirlo después
        efectosVisualesNodos.Add(efectoVisual);
    }

    // Método para realizar movimientos aleatorios
    private void MoverAleatoriamente()
    {
        if (!IsServer) return;
        Debug.Log("Iniciando movimientos aleatorios");
        // Iniciar coroutine solo si no está ya ejecutándose
        if (!efectoActivo)
        {
            efectoActivo = true;
            rutinaDeProcesamiento = StartCoroutine(ProcesarMovimientosAleatorios());
        }
    }

    private IEnumerator ProcesarMovimientosAleatorios()
    {
        for (int i = 0; i < movimientosPorTurno; i++)
        {
            // MODIFICADO: Obtener específicamente los ingredientes de los nodos afectados
            List<GameObject> ingredientesMovibles = ObtenerIngredientesMoviblesEnNodosAfectados();

            if (ingredientesMovibles.Count > 0)
            {
                // Elegir un ingrediente al azar para mover
                GameObject ingredienteAMover = ingredientesMovibles[Random.Range(0, ingredientesMovibles.Count)];
                List<GameObject> posiblesDestinos = ObtenerNodosVacios();

                if (posiblesDestinos.Count > 0)
                {
                    // Elegir destino aleatorio
                    GameObject nodoDestino = posiblesDestinos[Random.Range(0, posiblesDestinos.Count)];

                    // Ejecutar movimiento
                    MoverIngredienteServerRpc(
                        ingredienteAMover.GetComponent<NetworkObject>().NetworkObjectId,
                        nodoDestino.GetComponent<NetworkObject>().NetworkObjectId
                    );

                    // Esperar para el próximo movimiento
                    yield return new WaitForSeconds(delayEntreMovimientos);
                }
            }
        }

        efectoActivo = false;
    }

    // NUEVO: Método para obtener específicamente los ingredientes de los nodos afectados
    private List<GameObject> ObtenerIngredientesMoviblesEnNodosAfectados()
    {
        List<GameObject> ingredientes = new List<GameObject>();

        foreach (var nodoObj in nodosAfectados)
        {
            if (nodoObj == null) continue;

            Node nodo = nodoObj.GetComponent<Node>();
            if (nodo != null && nodo.hasIngredient.Value && nodo.currentIngredient != null && nodo.PuedeMoverse())
            {
                ingredientes.Add(nodo.currentIngredient);
            }
        }

        return ingredientes;
    }

    // Obtener nodos con ingredientes que se pueden mover
    private List<GameObject> ObtenerNodosConIngredientesMovibles()
    {
        List<GameObject> resultado = new List<GameObject>();

        // Necesitamos el NodeMap para buscar nodos
        Node nodoOrigenComp = nodoOrigen.GetComponent<Node>();
        if (nodoOrigenComp == null || nodoOrigenComp.nodeMap == null) return resultado;

        foreach (var nodo in nodoOrigenComp.nodeMap.nodesList)
        {
            Node nodoComp = nodo.GetComponent<Node>();
            if (nodoComp != null && nodoComp.hasIngredient.Value && nodoComp.PuedeMoverse())
            {
                resultado.Add(nodo);
            }
        }

        return resultado;
    }

    // Obtener nodos vacíos disponibles
    private List<GameObject> ObtenerNodosVacios()
    {
        List<GameObject> nodosVacios = new List<GameObject>();

        Node nodoComp = nodoOrigen.GetComponent<Node>();
        if (nodoComp == null || nodoComp.nodeMap == null) return nodosVacios;

        foreach (var nodo in nodoComp.nodeMap.nodesList)
        {
            Node posibleNodo = nodo.GetComponent<Node>();
            if (posibleNodo != null && !posibleNodo.hasIngredient.Value)
            {
                nodosVacios.Add(nodo);
            }
        }

        return nodosVacios;
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoverIngredienteServerRpc(ulong ingredienteId, ulong destinoId)
    {
        // Localizar objetos por NetworkObjectId
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ingredienteId, out NetworkObject ingredienteObj) ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(destinoId, out NetworkObject destinoObj))
        {
            Debug.LogError("No se pudieron encontrar los objetos por ID");
            return;
        }

        GameObject ingrediente = ingredienteObj.gameObject;
        GameObject nodoDestino = destinoObj.gameObject;

        // Buscar el nodo que contiene este ingrediente
        Node nodoOrigen = null;
        NodeMap nodeMap = FindObjectOfType<NodeMap>();
        if (nodeMap != null)
        {
            foreach (var nodo in nodeMap.nodesList)
            {
                Node comp = nodo.GetComponent<Node>();
                if (comp != null && comp.hasIngredient.Value && comp.currentIngredient == ingrediente)
                {
                    nodoOrigen = comp;
                    break;
                }
            }
        }

        if (nodoOrigen == null)
        {
            Debug.LogError("No se pudo encontrar el nodo origen del ingrediente");
            return;
        }

        Node nodoDestinoComp = nodoDestino.GetComponent<Node>();
        if (nodoDestinoComp == null || nodoDestinoComp.hasIngredient.Value)
        {
            Debug.LogWarning("El nodo destino no es válido o ya tiene un ingrediente");
            return;
        }

        // Verificar que el ingrediente puede moverse
        if (!nodoOrigen.PuedeMoverse())
        {
            Debug.LogWarning("El ingrediente no puede moverse");
            return;
        }

        // Mover ingrediente en el servidor
        // 1. Limpiar nodo origen
        nodoOrigen.hasIngredient.Value = false;
        nodoOrigen.currentIngredient = null;

        // 2. Mover el ingrediente físicamente
        Vector3 posOrigen = ingrediente.transform.position;
        Vector3 posDestino = nodoDestino.transform.position;
        ingrediente.transform.position = posDestino;

        // 3. Actualizar nodo destino
        nodoDestinoComp.hasIngredient.Value = true;
        nodoDestinoComp.currentIngredient = ingrediente;

        // 4. Notificar a todos los clientes
        MoverIngredienteClientRpc(
            nodoOrigen.gameObject.GetComponent<NetworkObject>().NetworkObjectId,
            destinoId,
            ingredienteId
        );

        // 5. Reproducir efectos de sonido o visuales
        MostrarEfectoMovimientoClientRpc(
            posOrigen,
            posDestino
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
        // Los valores de NetworkVariable ya se sincronizarán automáticamente
        nodoDestino.currentIngredient = ingredienteObj.gameObject;
        nodoOrigen.currentIngredient = null;
    }

    [ClientRpc]
    private void MostrarEfectoMovimientoClientRpc(Vector3 posOrigen, Vector3 posDestino)
    {
        // Crear un efecto visual de movimiento (línea, partículas, etc)
        GameObject efecto = new GameObject("EfectoMovimiento");
        LineRenderer linea = efecto.AddComponent<LineRenderer>();
        linea.startWidth = 0.1f;
        linea.endWidth = 0.1f;
        linea.positionCount = 2;
        linea.SetPosition(0, posOrigen + Vector3.up * 0.5f);
        linea.SetPosition(1, posDestino + Vector3.up * 0.5f);

        // Usar colores vibrantes
        linea.startColor = new Color(1f, 0.5f, 0f); // Naranja
        linea.endColor = new Color(1f, 0f, 0f);     // Rojo

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

        // Ejecutar movimientos para este turno
        MoverAleatoriamente();

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

        // Destruir efectos visuales de nodos
        foreach (var efecto in efectosVisualesNodos)
        {
            if (efecto != null)
            {
                Destroy(efecto);
            }
        }
        efectosVisualesNodos.Clear();
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Si es el servidor, despawneamos el objeto
        if (IsServer && gameObject.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
    }
}