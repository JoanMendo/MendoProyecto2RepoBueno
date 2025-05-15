using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EfectoBlancoManager : NetworkBehaviour, IEfectoManager
{
    [Header("Configuración")]
    [SerializeField] private Color colorLinea = Color.white;
    [SerializeField] private float anchoLinea = 0.1f;
    [SerializeField] private float alturaLinea = 0.2f;
    [SerializeField] private bool permitirMovimientoAutomatico = true;
    [SerializeField] private float tiempoEntreMovimientos = 1.0f;
    [SerializeField] private bool mostrarDebug = true; // Activamos debug por defecto

    // Referencias
    public GameObject nodoOrigen;
    public GameObject nodoDestino;
    private S_Blanca efectoConfigurado;
    private int duracionRestante;

    // Referencias a líneas visuales
    private GameObject lineaVisual;
    private LineRenderer lineRenderer;

    // Control de estado
    private bool conexionActiva = false;
    private Coroutine rutinaMovimientoAutomatico;

    // Implementación de la interfaz IEfectoManager
    public void ConfigurarConEfecto(Efectos efecto)
    {
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: ConfigurarConEfecto llamado con {efecto?.name ?? "null"}");

        if (efecto is S_Blanca efectoBlanca)
        {
            efectoConfigurado = efectoBlanca;
            duracionRestante = efecto.duracion;

            if (mostrarDebug) Debug.Log($"EfectoBlancoManager: Configurado con éxito, duración: {duracionRestante}");

            // Configurar componentes visuales si no se ha hecho en Awake
            if (lineaVisual == null)
            {
                ConfigurarLineaVisual();
            }
        }
        else
        {
            Debug.LogError("Se intentó configurar EfectoBlancoManager con un efecto que no es S_Blanca");
        }
    }

    public void IniciarEfecto(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: IniciarEfecto llamado con nodoOrigen: {nodoOrigen?.name ?? "null"}, nodosAfectados: {(nodosAfectados?.Count ?? 0)}");

        // Para S_Blanca, si recibimos nodos adicionales, el primero será el destino
        GameObject nodoDestino = null;

        if (nodosAfectados != null && nodosAfectados.Count > 0)
        {
            nodoDestino = nodosAfectados[0];
            if (mostrarDebug) Debug.Log($"EfectoBlancoManager: Usando nodo destino de nodosAfectados: {nodoDestino.name}");
        }
        else
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: No se recibieron nodos adicionales para la conexión");
            return;
        }

        // Iniciar el efecto de conexión entre los dos nodos
        IniciarEfectoConexion(nodoOrigen, nodoDestino, efectoConfigurado, duracionRestante);
    }

    public bool ValidarNodos(List<GameObject> nodosSeleccionados)
    {
        // S_Blanca necesita exactamente 2 nodos
        if (nodosSeleccionados == null || nodosSeleccionados.Count != 2)
        {
            if (mostrarDebug) Debug.LogWarning($"EfectoBlancoManager: ValidarNodos - Cantidad incorrecta de nodos: {(nodosSeleccionados?.Count ?? 0)}, se necesitan 2");
            return false;
        }

        // Verificar que ambos nodos existen
        bool resultado = nodosSeleccionados[0] != null && nodosSeleccionados[1] != null;
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: ValidarNodos - Resultado: {resultado}");
        return resultado;
    }

    public void EjecutarAccion(List<GameObject> nodosSeleccionados)
    {
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: EjecutarAccion llamado con {nodosSeleccionados?.Count ?? 0} nodos");

        if (!ValidarNodos(nodosSeleccionados))
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: Validación de nodos fallida");
            return;
        }

        nodoOrigen = nodosSeleccionados[0];
        nodoDestino = nodosSeleccionados[1];

        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: Nodos validados - Origen: {nodoOrigen.name}, Destino: {nodoDestino.name}");

        // Asegurar que tiene NetworkObject
        if (GetComponent<NetworkObject>() == null)
        {
            gameObject.AddComponent<NetworkObject>();
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Añadido NetworkObject faltante");
        }

        // Iniciar el efecto
        IniciarEfectoConexion(nodoOrigen, nodoDestino, efectoConfigurado, duracionRestante);
    }

    private void Awake()
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: Awake");

        // Configurar componentes visuales
        ConfigurarLineaVisual();
    }

    private void ConfigurarLineaVisual()
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: ConfigurarLineaVisual");

        // Crear objeto hijo para línea visual
        lineaVisual = new GameObject("LineaConexion");
        lineaVisual.transform.SetParent(transform);

        // Añadir LineRenderer
        lineRenderer = lineaVisual.AddComponent<LineRenderer>();
        lineRenderer.startWidth = anchoLinea;
        lineRenderer.endWidth = anchoLinea;
        lineRenderer.positionCount = 2;

        // Usar colores definidos
        lineRenderer.startColor = colorLinea;
        lineRenderer.endColor = colorLinea;

        // Configurar material
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Ocultar línea inicialmente
        lineRenderer.enabled = false;

        if (mostrarDebug) Debug.Log("EfectoBlancoManager: LineRenderer configurado correctamente");
    }

    // Método principal para iniciar el efecto de conexión
    public void IniciarEfectoConexion(GameObject origen, GameObject destino, S_Blanca efecto, int duracion)
    {
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: IniciarEfectoConexion - Origen: {origen?.name ?? "null"}, Destino: {destino?.name ?? "null"}, Efecto: {efecto?.name ?? "null"}, Duración: {duracion}");

        // Validaciones
        if (origen == null)
        {
            Debug.LogError("EfectoBlancoManager: nodoOrigen es null");
            return;
        }

        if (destino == null)
        {
            Debug.LogError("EfectoBlancoManager: nodoDestino es null");
            return;
        }

        if (efecto == null)
        {
            Debug.LogError("EfectoBlancoManager: efecto es null");
            return;
        }

        // Guardar referencias
        nodoOrigen = origen;
        nodoDestino = destino;
        efectoConfigurado = efecto;
        duracionRestante = duracion;

        // Convertirse en objeto de red si no lo es ya
        if (!IsSpawned && GetComponent<NetworkObject>() != null)
        {
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Spawning NetworkObject");
            GetComponent<NetworkObject>().Spawn();
        }

        // Activar y actualizar línea visual
        ActualizarLineaVisual();

        conexionActiva = true;

        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: Conexión establecida, conexionActiva = {conexionActiva}, duracionRestante = {duracionRestante}");

        // Activar movimiento automático si está configurado
        if (permitirMovimientoAutomatico && IsServer)
        {
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Iniciando coroutine para movimiento automático");
            rutinaMovimientoAutomatico = StartCoroutine(ProcesarMovimientoAutomatico());
        }
        else
        {
            if (mostrarDebug) Debug.Log($"EfectoBlancoManager: Movimiento automático NO iniciado - permitirMovimientoAutomatico: {permitirMovimientoAutomatico}, IsServer: {IsServer}");
        }

        // Notificar a los clientes
        if (IsServer)
        {
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Notificando a clientes");
            MostrarConexionClientRpc(
                nodoOrigen.transform.position,
                nodoDestino.transform.position
            );
        }

        Debug.Log($"Conexión S_Blanca iniciada entre {nodoOrigen.name} y {nodoDestino.name}");
    }

    private void ActualizarLineaVisual()
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: ActualizarLineaVisual");

        if (lineRenderer == null)
        {
            Debug.LogError("EfectoBlancoManager: lineRenderer es null");
            return;
        }

        if (nodoOrigen == null || nodoDestino == null)
        {
            Debug.LogError($"EfectoBlancoManager: nodoOrigen o nodoDestino es null - Origen: {nodoOrigen?.name ?? "null"}, Destino: {nodoDestino?.name ?? "null"}");
            return;
        }

        // Mostrar línea
        lineRenderer.enabled = true;

        // Establecer posiciones
        lineRenderer.SetPosition(0, nodoOrigen.transform.position + Vector3.up * alturaLinea);
        lineRenderer.SetPosition(1, nodoDestino.transform.position + Vector3.up * alturaLinea);

        if (mostrarDebug) Debug.Log("EfectoBlancoManager: Línea visual actualizada");
    }

    // Coroutine para mover ingredientes automáticamente entre los nodos conectados
    private IEnumerator ProcesarMovimientoAutomatico()
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: Iniciando ProcesarMovimientoAutomatico");

        while (conexionActiva && duracionRestante > 0)
        {
            Debug.Log($"Procesando movimiento automático - conexionActiva: {conexionActiva}, duracionRestante: {duracionRestante}");

            // Esperar tiempo configurado
            yield return new WaitForSeconds(tiempoEntreMovimientos);

            // Intentar mover ingredientes entre los nodos
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Llamando a MoverIngredientesConectados");
            MoverIngredientesConectados();
        }

        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: Saliendo de ProcesarMovimientoAutomatico - conexionActiva: {conexionActiva}, duracionRestante: {duracionRestante}");
    }

    public void MoverIngredientesConectados()
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: MoverIngredientesConectados - INICIO");

        if (!IsServer)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: MoverIngredientesConectados - No es el servidor, saliendo");
            return;
        }

        // Verificar que tenemos ambos nodos
        if (nodoOrigen == null || nodoDestino == null)
        {
            if (mostrarDebug) Debug.LogError($"EfectoBlancoManager: Nodos faltantes - Origen: {nodoOrigen?.name ?? "null"}, Destino: {nodoDestino?.name ?? "null"}");
            return;
        }

        Node origen = nodoOrigen.GetComponent<Node>();
        Node destino = nodoDestino.GetComponent<Node>();

        if (origen == null || destino == null)
        {
            if (mostrarDebug) Debug.LogError("EfectoBlancoManager: Componentes Node faltantes");
            return;
        }

        // Verificar si hay ingredientes en los nodos
        bool origenTieneIngrediente = origen.hasIngredient.Value;
        bool destinoTieneIngrediente = destino.hasIngredient.Value;

        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: Estado de los nodos - Origen tiene ingrediente: {origenTieneIngrediente}, Destino tiene ingrediente: {destinoTieneIngrediente}");

        // ÚNICO CASO: Verificar si el origen tiene un ingrediente
        if (origenTieneIngrediente && origen.PuedeMoverse())
        {
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Origen tiene un ingrediente que puede moverse");

            // Verificar NetworkObjects
            NetworkObject origenNetObj = origen.GetComponent<NetworkObject>();
            NetworkObject destinoNetObj = destino.GetComponent<NetworkObject>();

            if (origenNetObj == null || !origenNetObj.IsSpawned || destinoNetObj == null || !destinoNetObj.IsSpawned)
            {
                if (mostrarDebug) Debug.LogError("EfectoBlancoManager: NetworkObjects inválidos para los nodos");
                return;
            }

            // Si el destino tiene un ingrediente, primero limpiarlo
            if (destinoTieneIngrediente)
            {
                if (mostrarDebug) Debug.Log("EfectoBlancoManager: Destino tiene ingrediente, destruyéndolo primero");
                destino.ClearNodeIngredient();

                // Esperar un fotograma para asegurarse de que se complete la limpieza
                // (Esto no es necesario en la coroutine, pero es buena práctica)
            }

            // Ahora mover el ingrediente del origen al destino
            MoverIngredienteUnidireccionalServerRpc(
                origenNetObj.NetworkObjectId,
                destinoNetObj.NetworkObjectId
            );
        }
        else
        {
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Origen no tiene ingrediente o no puede moverse, no se realiza ninguna acción");
        }

        if (mostrarDebug) Debug.Log("EfectoBlancoManager: MoverIngredientesConectados - FIN");
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoverIngredienteUnidireccionalServerRpc(ulong origenId, ulong destinoId)
    {
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: MoverIngredienteUnidireccionalServerRpc - Origen ID: {origenId}, Destino ID: {destinoId}");

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

        // Verificar que el nodo origen tenga ingrediente
        if (!nodoOrigen.hasIngredient.Value)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: El nodo origen ya no tiene ingrediente");
            return;
        }

        // Verificar que el nodo destino no tenga ingrediente
        if (nodoDestino.hasIngredient.Value)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: El nodo destino todavía tiene ingrediente, limpiándolo nuevamente");
            nodoDestino.ClearNodeIngredient();
        }

        // Obtener los datos del ingrediente
        GameObject ingredienteOrigen = nodoOrigen.currentIngredient;
        if (ingredienteOrigen == null)
        {
            if (mostrarDebug) Debug.LogError("EfectoBlancoManager: Ingrediente origen es null a pesar de hasIngredient=true");
            return;
        }

        // Verificar que el ingrediente tiene componente
        componente comp = ingredienteOrigen.GetComponent<componente>();
        if (comp == null || comp.data == null || comp.data.prefab3D == null)
        {
            if (mostrarDebug) Debug.LogError("EfectoBlancoManager: Ingrediente origen no tiene datos válidos");
            return;
        }

        // Guardar datos importantes
        GameObject prefabIngrediente = comp.data.prefab3D;
        Vector3 posOrigen = ingredienteOrigen.transform.position;
        Vector3 posDestino = nodoDestino.transform.position;

        // 1. Limpiar nodo origen
        nodoOrigen.ClearNodeIngredient();

        // 2. Colocar el mismo tipo de ingrediente en el destino
        nodoDestino.SetNodeIngredient(prefabIngrediente);

        // 3. Mostrar efecto visual
        MostrarEfectoMovimientoUnidireccionalClientRpc(
            posOrigen,
            posDestino
        );

        if (mostrarDebug) Debug.Log("EfectoBlancoManager: Ingrediente movido con éxito del origen al destino");
    }

    [ClientRpc]
    private void MostrarEfectoMovimientoUnidireccionalClientRpc(Vector3 posOrigen, Vector3 posDestino)
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: MostrarEfectoMovimientoUnidireccionalClientRpc");

        // Crear efecto visual específico para movimiento unidireccional
        GameObject efecto = new GameObject("EfectoMovimientoBlanco");
        LineRenderer linea = efecto.AddComponent<LineRenderer>();
        linea.startWidth = anchoLinea * 1.5f;
        linea.endWidth = anchoLinea * 1.5f;
        linea.positionCount = 2;
        linea.SetPosition(0, posOrigen + Vector3.up * alturaLinea);
        linea.SetPosition(1, posDestino + Vector3.up * alturaLinea);

        // Usar colores distintivos para movimiento unidireccional
        linea.startColor = new Color(1f, 1f, 1f); // Blanco
        linea.endColor = new Color(0.5f, 0.5f, 1f); // Azul claro

        // Asignar material
        linea.material = new Material(Shader.Find("Sprites/Default"));

        // Animar y destruir
        StartCoroutine(AnimarLineaMovimiento(linea));
    }

    [ServerRpc(RequireOwnership = false)]
    private void IntercambiarIngredientesServerRpc(ulong origenId, ulong destinoId)
    {
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: IntercambiarIngredientesServerRpc - Origen ID: {origenId}, Destino ID: {destinoId}");

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

        // Verificar que ambos nodos tengan ingredientes
        if (!nodoOrigen.hasIngredient.Value || !nodoDestino.hasIngredient.Value)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: Al menos uno de los nodos no tiene ingrediente");
            return;
        }

        // Obtener los ingredientes
        GameObject ingredienteOrigen = nodoOrigen.currentIngredient;
        GameObject ingredienteDestino = nodoDestino.currentIngredient;

        if (ingredienteOrigen == null || ingredienteDestino == null)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: Al menos uno de los ingredientes es null");
            return;
        }

        // Verificar NetworkObjects de los ingredientes
        NetworkObject netObjOrigen = ingredienteOrigen.GetComponent<NetworkObject>();
        NetworkObject netObjDestino = ingredienteDestino.GetComponent<NetworkObject>();

        if (netObjOrigen == null || !netObjOrigen.IsSpawned || netObjDestino == null || !netObjDestino.IsSpawned)
        {
            if (mostrarDebug) Debug.LogError("EfectoBlancoManager: NetworkObjects inválidos para los ingredientes");
            return;
        }

        // Obtener IDs para la sincronización
        ulong idIngredienteOrigen = netObjOrigen.NetworkObjectId;
        ulong idIngredienteDestino = netObjDestino.NetworkObjectId;

        // Intercambiar posiciones físicas
        Vector3 posOrigen = ingredienteOrigen.transform.position;
        Vector3 posDestino = ingredienteDestino.transform.position;

        ingredienteOrigen.transform.position = posDestino;
        ingredienteDestino.transform.position = posOrigen;

        // Intercambiar referencias
        nodoOrigen.currentIngredient = ingredienteDestino;
        nodoDestino.currentIngredient = ingredienteOrigen;

        if (mostrarDebug) Debug.Log("EfectoBlancoManager: Ingredientes intercambiados con éxito");

        // Notificar a los clientes
        IntercambiarIngredientesClientRpc(
            origenId,
            destinoId,
            idIngredienteOrigen,
            idIngredienteDestino
        );

        // Efectos visuales
        MostrarEfectoIntercambioClientRpc(posOrigen, posDestino);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoverIngredienteServerRpc(ulong origenId, ulong destinoId)
    {
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: MoverIngredienteServerRpc - Origen ID: {origenId}, Destino ID: {destinoId}");

        // Similar a implementaciones anteriores
        NetworkObject origenObj = null;
        NetworkObject destinoObj = null;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out origenObj) ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(destinoId, out destinoObj))
        {
            if (mostrarDebug) Debug.LogError("EfectoBlancoManager: No se encontraron los objetos de red");
            return;
        }

        Node nodoOrigen = origenObj.GetComponent<Node>();
        Node nodoDestino = destinoObj.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null)
        {
            if (mostrarDebug) Debug.LogError("EfectoBlancoManager: Los objetos no tienen componente Node");
            return;
        }

        // Si el nodo destino ya tiene ingrediente o el origen no tiene, no hacer nada
        if (nodoDestino.hasIngredient.Value || !nodoOrigen.hasIngredient.Value)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: Destino ya tiene ingrediente o origen no tiene ingrediente");
            return;
        }

        // Mover ingrediente
        GameObject ingrediente = nodoOrigen.currentIngredient;
        if (ingrediente == null)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: Ingrediente es null");
            return;
        }

        // Verificar NetworkObject
        NetworkObject ingredienteNetObj = ingrediente.GetComponent<NetworkObject>();
        if (ingredienteNetObj == null || !ingredienteNetObj.IsSpawned)
        {
            if (mostrarDebug) Debug.LogError("EfectoBlancoManager: NetworkObject del ingrediente inválido");
            return;
        }

        // Obtener ID para sincronización
        ulong idIngrediente = ingredienteNetObj.NetworkObjectId;

        // 1. Limpiar nodo origen
        nodoOrigen.hasIngredient.Value = false;
        nodoOrigen.currentIngredient = null;

        // 2. Mover el ingrediente físicamente
        Vector3 posOrigen = ingrediente.transform.position;
        Vector3 posDestino = nodoDestino.transform.position;
        ingrediente.transform.position = posDestino;

        // 3. Actualizar nodo destino
        nodoDestino.hasIngredient.Value = true;
        nodoDestino.currentIngredient = ingrediente;

        if (mostrarDebug) Debug.Log("EfectoBlancoManager: Ingrediente movido con éxito");

        // 4. Notificar a todos los clientes
        MoverIngredienteClientRpc(
            origenId,
            destinoId,
            idIngrediente
        );

        // 5. Mostrar efecto visual
        MostrarEfectoMovimientoClientRpc(posOrigen, posDestino);
    }

    [ClientRpc]
    private void IntercambiarIngredientesClientRpc(ulong origenId, ulong destinoId,
                                                 ulong idIngredienteOrigen, ulong idIngredienteDestino)
    {
        if (mostrarDebug && !IsServer) Debug.Log("EfectoBlancoManager: IntercambiarIngredientesClientRpc");

        // Solo ejecutar en clientes (no en servidor)
        if (IsServer) return;

        // Encontrar objetos por ID
        NetworkObject origenObj = null;
        NetworkObject destinoObj = null;
        NetworkObject ingredienteOrigenObj = null;
        NetworkObject ingredienteDestinoObj = null;

        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out origenObj);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(destinoId, out destinoObj);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idIngredienteOrigen, out ingredienteOrigenObj);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idIngredienteDestino, out ingredienteDestinoObj);

        if (origenObj == null || destinoObj == null ||
            ingredienteOrigenObj == null || ingredienteDestinoObj == null)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: Objetos no encontrados en el cliente");
            return;
        }

        Node nodoOrigen = origenObj.GetComponent<Node>();
        Node nodoDestino = destinoObj.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: Componentes Node no encontrados en el cliente");
            return;
        }

        // Actualizar referencias en clientes
        nodoOrigen.currentIngredient = ingredienteDestinoObj.gameObject;
        nodoDestino.currentIngredient = ingredienteOrigenObj.gameObject;

        if (mostrarDebug) Debug.Log("EfectoBlancoManager: Referencias actualizadas en el cliente");
    }

    [ClientRpc]
    private void MoverIngredienteClientRpc(ulong origenId, ulong destinoId, ulong ingredienteId)
    {
        if (mostrarDebug && !IsServer) Debug.Log("EfectoBlancoManager: MoverIngredienteClientRpc");

        // Solo ejecutar en clientes
        if (IsServer) return;

        // Encontrar objetos por ID
        NetworkObject origenObj = null;
        NetworkObject destinoObj = null;
        NetworkObject ingredienteObj = null;

        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out origenObj);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(destinoId, out destinoObj);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ingredienteId, out ingredienteObj);

        if (origenObj == null || destinoObj == null || ingredienteObj == null)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: Objetos no encontrados en el cliente");
            return;
        }

        Node nodoOrigen = origenObj.GetComponent<Node>();
        Node nodoDestino = destinoObj.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null)
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: Componentes Node no encontrados en el cliente");
            return;
        }

        // Actualizar referencias en clientes
        nodoDestino.currentIngredient = ingredienteObj.gameObject;
        nodoOrigen.currentIngredient = null;

        if (mostrarDebug) Debug.Log("EfectoBlancoManager: Referencias actualizadas en el cliente");
    }

    [ClientRpc]
    private void MostrarConexionClientRpc(Vector3 posOrigen, Vector3 posDestino)
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: MostrarConexionClientRpc");

        // Actualizar línea visual para todos los clientes
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, posOrigen + Vector3.up * alturaLinea);
            lineRenderer.SetPosition(1, posDestino + Vector3.up * alturaLinea);
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Línea visual actualizada en cliente");
        }
        else
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: lineRenderer es null");
        }

        // Crear efecto de partículas en ambos extremos
        CrearEfectoParticulas(posOrigen + Vector3.up * alturaLinea);
        CrearEfectoParticulas(posDestino + Vector3.up * alturaLinea);
    }

    private void CrearEfectoParticulas(Vector3 posicion)
    {
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: CrearEfectoParticulas en {posicion}");

        // Crear sistema de partículas simple
        GameObject partObj = new GameObject("EfectoConexion");
        partObj.transform.position = posicion;

        ParticleSystem ps = partObj.AddComponent<ParticleSystem>();

        // Configuración básica
        var main = ps.main;
        main.startSize = 0.2f;
        main.startLifetime = 1f;
        main.startColor = colorLinea;

        // Auto-destruir después de un tiempo
        Destroy(partObj, 2f);
    }

    [ClientRpc]
    private void MostrarEfectoMovimientoClientRpc(Vector3 posOrigen, Vector3 posDestino)
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: MostrarEfectoMovimientoClientRpc");

        // Crear efecto visual pulsante de movimiento
        GameObject efecto = new GameObject("EfectoMovimientoBlanco");
        LineRenderer linea = efecto.AddComponent<LineRenderer>();
        linea.startWidth = anchoLinea * 1.5f;
        linea.endWidth = anchoLinea * 1.5f;
        linea.positionCount = 2;
        linea.SetPosition(0, posOrigen + Vector3.up * alturaLinea);
        linea.SetPosition(1, posDestino + Vector3.up * alturaLinea);

        // Usar colores vibrantes para diferenciar del efecto normal
        linea.startColor = new Color(1f, 1f, 0.5f); // Amarillo
        linea.endColor = colorLinea;

        // Asignar material
        linea.material = new Material(Shader.Find("Sprites/Default"));

        // Animar y destruir
        StartCoroutine(AnimarLineaMovimiento(linea));
    }

    [ClientRpc]
    private void MostrarEfectoIntercambioClientRpc(Vector3 posOrigen, Vector3 posDestino)
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: MostrarEfectoIntercambioClientRpc");

        // Crear efecto visual específico para intercambio
        GameObject efectoA = new GameObject("EfectoIntercambioA");
        GameObject efectoB = new GameObject("EfectoIntercambioB");

        // Línea A -> B
        LineRenderer lineaA = efectoA.AddComponent<LineRenderer>();
        lineaA.startWidth = anchoLinea;
        lineaA.endWidth = anchoLinea;
        lineaA.positionCount = 2;
        lineaA.SetPosition(0, posOrigen + Vector3.up * alturaLinea);
        lineaA.SetPosition(1, posDestino + Vector3.up * alturaLinea);

        // Línea B -> A
        LineRenderer lineaB = efectoB.AddComponent<LineRenderer>();
        lineaB.startWidth = anchoLinea;
        lineaB.endWidth = anchoLinea;
        lineaB.positionCount = 2;
        lineaB.SetPosition(0, posDestino + Vector3.up * alturaLinea);
        lineaB.SetPosition(1, posOrigen + Vector3.up * alturaLinea);

        // Usar colores distintos
        lineaA.startColor = new Color(1f, 1f, 0.5f); // Amarillo
        lineaA.endColor = new Color(0f, 1f, 1f);    // Cyan

        lineaB.startColor = new Color(0f, 1f, 1f);   // Cyan
        lineaB.endColor = new Color(1f, 1f, 0.5f);  // Amarillo

        // Asignar material
        lineaA.material = new Material(Shader.Find("Sprites/Default"));
        lineaB.material = new Material(Shader.Find("Sprites/Default"));

        // Animar y destruir
        StartCoroutine(AnimarLineaMovimiento(lineaA));
        StartCoroutine(AnimarLineaMovimiento(lineaB));
    }

    private IEnumerator AnimarLineaMovimiento(LineRenderer linea)
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: AnimarLineaMovimiento iniciado");

        // Animar intensidad durante 0.5 segundos
        float duracion = 0.5f;
        float tiempoInicio = Time.time;

        while (Time.time - tiempoInicio < duracion)
        {
            // Calcular factor de animación
            float t = (Time.time - tiempoInicio) / duracion;

            // Hacer parpadear la línea
            linea.startColor = new Color(
                linea.startColor.r,
                linea.startColor.g,
                linea.startColor.b,
                Mathf.PingPong(t * 4, 1)
            );

            linea.endColor = new Color(
                linea.endColor.r,
                linea.endColor.g,
                linea.endColor.b,
                Mathf.PingPong(t * 4 + 0.5f, 1)
            );

            yield return null;
        }

        // Destruir después de la animación
        Destroy(linea.gameObject);
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: Línea de animación destruida");
    }

    // Método llamado cada turno
    public void ProcesarTurno()
    {
        if (!IsServer) return;

        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: ProcesarTurno - duracionRestante antes: {duracionRestante}");

        // Reducir duración
        duracionRestante--;

        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: ProcesarTurno - duracionRestante después: {duracionRestante}");

        // Si es el último turno, limpiar
        if (duracionRestante <= 0)
        {
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Último turno completado, limpiando efecto");
            LimpiarEfecto();
        }
        else
        {
            // Si no tenemos movimiento automático, intentar mover al menos una vez por turno
            if (!permitirMovimientoAutomatico)
            {
                if (mostrarDebug) Debug.Log("EfectoBlancoManager: Intentando mover ingredientes una vez por turno");
                MoverIngredientesConectados();
            }
            else
            {
                if (mostrarDebug) Debug.Log("EfectoBlancoManager: Movimiento automático activo, no se fuerza movimiento extra");
            }
        }
    }

    // Método para limpiar el efecto
    public void LimpiarEfecto()
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: LimpiarEfecto llamado");

        // Detener el movimiento automático
        if (rutinaMovimientoAutomatico != null)
        {
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Deteniendo coroutine de movimiento automático");
            StopCoroutine(rutinaMovimientoAutomatico);
            rutinaMovimientoAutomatico = null;
        }

        conexionActiva = false;
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: Conexión desactivada, conexionActiva = {conexionActiva}");

        if (IsServer)
        {
            // Notificar a los clientes
            LimpiarEfectoClientRpc();

            // Programar destrucción
            StartCoroutine(DestruirDespuesDeDelay(0.5f));
        }
    }

    [ClientRpc]
    private void LimpiarEfectoClientRpc()
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: LimpiarEfectoClientRpc");

        // Ocultar línea visual
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Línea visual desactivada");
        }
        else
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: lineRenderer es null");
        }

        // Efecto final de "rotura" de la conexión
        if (nodoOrigen != null && nodoDestino != null)
        {
            Vector3 posA = nodoOrigen.transform.position + Vector3.up * alturaLinea;
            Vector3 posB = nodoDestino.transform.position + Vector3.up * alturaLinea;

            // Crear efecto de "rotura"
            GameObject efectoRotura = new GameObject("EfectoRotura");
            ParticleSystem ps = efectoRotura.AddComponent<ParticleSystem>();

            // Posicionar en el centro
            efectoRotura.transform.position = Vector3.Lerp(posA, posB, 0.5f);

            // Configurar partículas para que se expandan
            var main = ps.main;
            main.startSpeed = 3f;
            main.startSize = 0.1f;
            main.startColor = colorLinea;
            main.startLifetime = 1f;

            // Auto-destruir
            Destroy(efectoRotura, 2f);

            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Efecto de rotura creado");
        }
        else
        {
            if (mostrarDebug) Debug.LogWarning($"EfectoBlancoManager: Nodos inválidos - Origen: {nodoOrigen?.name ?? "null"}, Destino: {nodoDestino?.name ?? "null"}");
        }
    }

    private IEnumerator DestruirDespuesDeDelay(float delay)
    {
        if (mostrarDebug) Debug.Log($"EfectoBlancoManager: DestruirDespuesDeDelay con delay de {delay}s");

        yield return new WaitForSeconds(delay);

        // Si es el servidor, despawneamos el objeto
        if (IsServer && gameObject.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
        {
            if (mostrarDebug) Debug.Log("EfectoBlancoManager: Despawning NetworkObject");
            netObj.Despawn();
        }
        else
        {
            if (mostrarDebug) Debug.LogWarning("EfectoBlancoManager: No se puede despawnear el objeto");
        }
    }

    private void OnDestroy()
    {
        if (mostrarDebug) Debug.Log("EfectoBlancoManager: OnDestroy llamado");

        // Asegurar limpieza adecuada
        if (rutinaMovimientoAutomatico != null)
        {
            StopCoroutine(rutinaMovimientoAutomatico);
            rutinaMovimientoAutomatico = null;
        }

        // Limpiar recursos
        if (lineaVisual != null)
        {
            Destroy(lineaVisual);
        }
    }
}