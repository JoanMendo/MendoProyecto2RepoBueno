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
        if (efecto is S_Blanca efectoBlanca)
        {
            efectoConfigurado = efectoBlanca;
            duracionRestante = efecto.duracion;

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

    public bool ValidarNodos(List<GameObject> nodosSeleccionados)
    {
        // S_Blanca necesita exactamente 2 nodos
        if (nodosSeleccionados == null || nodosSeleccionados.Count != 2)
            return false;

        // Ambos nodos deben tener componente Node
        Node nodo1 = nodosSeleccionados[0].GetComponent<Node>();
        Node nodo2 = nodosSeleccionados[1].GetComponent<Node>();

        if (nodo1 == null || nodo2 == null)
            return false;

        // No deberían ser el mismo nodo
        if (nodo1 == nodo2)
            return false;

        // Aquí podrías añadir más validaciones específicas
        // Por ejemplo, comprobar si están adyacentes

        return true;
    }

    public void EjecutarAccion(List<GameObject> nodosSeleccionados)
    {
        if (!ValidarNodos(nodosSeleccionados))
            return;

        nodoOrigen = nodosSeleccionados[0];
        nodoDestino = nodosSeleccionados[1];

        // Asegurar que tiene NetworkObject
        if (GetComponent<NetworkObject>() == null)
        {
            gameObject.AddComponent<NetworkObject>();
        }

        // Iniciar el efecto
        IniciarEfectoConexion(nodoOrigen, nodoDestino, efectoConfigurado, duracionRestante);
    }

    private void Awake()
    {
        // Configurar componentes visuales
        ConfigurarLineaVisual();
    }

    private void ConfigurarLineaVisual()
    {
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
    }

    // Método principal para iniciar el efecto de conexión
    public void IniciarEfectoConexion(GameObject origen, GameObject destino, S_Blanca efecto, int duracion)
    {
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

        // Activar y actualizar línea visual
        ActualizarLineaVisual();

        // Activar movimiento automático si está configurado
        if (permitirMovimientoAutomatico && IsServer)
        {
            rutinaMovimientoAutomatico = StartCoroutine(ProcesarMovimientoAutomatico());
        }

        conexionActiva = true;

        // Notificar a los clientes
        if (IsServer)
        {
            MostrarConexionClientRpc(
                nodoOrigen.transform.position,
                nodoDestino.transform.position
            );
        }

        Debug.Log($"Conexión blanca iniciada entre {nodoOrigen.name} y {nodoDestino.name}");
    }

    private void ActualizarLineaVisual()
    {
        if (lineRenderer == null || nodoOrigen == null || nodoDestino == null) return;

        // Mostrar línea
        lineRenderer.enabled = true;

        // Establecer posiciones
        lineRenderer.SetPosition(0, nodoOrigen.transform.position + Vector3.up * alturaLinea);
        lineRenderer.SetPosition(1, nodoDestino.transform.position + Vector3.up * alturaLinea);
    }

    // Coroutine para mover ingredientes automáticamente entre los nodos conectados
    private IEnumerator ProcesarMovimientoAutomatico()
    {
        while (conexionActiva && duracionRestante > 0)
        {
            // Esperar tiempo configurado
            yield return new WaitForSeconds(tiempoEntreMovimientos);

            // Intentar mover ingredientes entre los nodos
            MoverIngredientesConectados();
        }
    }

    public void MoverIngredientesConectados()
    {
        if (!IsServer) return;

        // Verificar que tenemos ambos nodos
        if (nodoOrigen == null || nodoDestino == null) return;

        Node origen = nodoOrigen.GetComponent<Node>();
        Node destino = nodoDestino.GetComponent<Node>();

        if (origen == null || destino == null) return;

        // Verificar si hay ingredientes en los nodos
        bool origenTieneIngrediente = origen.hasIngredient.Value;
        bool destinoTieneIngrediente = destino.hasIngredient.Value;

        // Casos posibles:
        // 1. Ambos nodos tienen ingredientes -> Intercambiar
        if (origenTieneIngrediente && destinoTieneIngrediente)
        {
            // Verificar si ambos pueden moverse
            if (origen.PuedeMoverse() && destino.PuedeMoverse())
            {
                IntercambiarIngredientesServerRpc(
                    origen.GetComponent<NetworkObject>().NetworkObjectId,
                    destino.GetComponent<NetworkObject>().NetworkObjectId
                );
            }
        }
        // 2. Solo origen tiene ingrediente y puede moverse -> Mover a destino
        else if (origenTieneIngrediente && origen.PuedeMoverse())
        {
            MoverIngredienteServerRpc(
                origen.GetComponent<NetworkObject>().NetworkObjectId,
                destino.GetComponent<NetworkObject>().NetworkObjectId
            );
        }
        // 3. Solo destino tiene ingrediente y puede moverse -> Mover a origen
        else if (destinoTieneIngrediente && destino.PuedeMoverse())
        {
            MoverIngredienteServerRpc(
                destino.GetComponent<NetworkObject>().NetworkObjectId,
                origen.GetComponent<NetworkObject>().NetworkObjectId
            );
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void IntercambiarIngredientesServerRpc(ulong origenId, ulong destinoId)
    {
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
            return;
        }

        // Obtener los ingredientes
        GameObject ingredienteOrigen = nodoOrigen.currentIngredient;
        GameObject ingredienteDestino = nodoDestino.currentIngredient;

        if (ingredienteOrigen == null || ingredienteDestino == null)
        {
            return;
        }

        // Obtener IDs para la sincronización
        ulong idIngredienteOrigen = ingredienteOrigen.GetComponent<NetworkObject>().NetworkObjectId;
        ulong idIngredienteDestino = ingredienteDestino.GetComponent<NetworkObject>().NetworkObjectId;

        // Intercambiar posiciones físicas
        Vector3 posOrigen = ingredienteOrigen.transform.position;
        Vector3 posDestino = ingredienteDestino.transform.position;

        ingredienteOrigen.transform.position = posDestino;
        ingredienteDestino.transform.position = posOrigen;

        // Intercambiar referencias
        nodoOrigen.currentIngredient = ingredienteDestino;
        nodoDestino.currentIngredient = ingredienteOrigen;

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
        // Similar a implementaciones anteriores
        NetworkObject origenObj = null;
        NetworkObject destinoObj = null;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(origenId, out origenObj) ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(destinoId, out destinoObj))
        {
            return;
        }

        Node nodoOrigen = origenObj.GetComponent<Node>();
        Node nodoDestino = destinoObj.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null)
        {
            return;
        }

        // Si el nodo destino ya tiene ingrediente o el origen no tiene, no hacer nada
        if (nodoDestino.hasIngredient.Value || !nodoOrigen.hasIngredient.Value)
        {
            return;
        }

        // Mover ingrediente
        GameObject ingrediente = nodoOrigen.currentIngredient;
        if (ingrediente == null) return;

        // Obtener ID para sincronización
        ulong idIngrediente = ingrediente.GetComponent<NetworkObject>().NetworkObjectId;

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
            ingredienteOrigenObj == null || ingredienteDestinoObj == null) return;

        Node nodoOrigen = origenObj.GetComponent<Node>();
        Node nodoDestino = destinoObj.GetComponent<Node>();

        if (nodoOrigen == null || nodoDestino == null) return;

        // Actualizar referencias en clientes
        nodoOrigen.currentIngredient = ingredienteDestinoObj.gameObject;
        nodoDestino.currentIngredient = ingredienteOrigenObj.gameObject;
    }

    [ClientRpc]
    private void MoverIngredienteClientRpc(ulong origenId, ulong destinoId, ulong ingredienteId)
    {
        // Solo ejecutar en clientes
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
    private void MostrarConexionClientRpc(Vector3 posOrigen, Vector3 posDestino)
    {
        // Actualizar línea visual para todos los clientes
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, posOrigen + Vector3.up * alturaLinea);
            lineRenderer.SetPosition(1, posDestino + Vector3.up * alturaLinea);
        }

        // Crear efecto de partículas en ambos extremos
        CrearEfectoParticulas(posOrigen + Vector3.up * alturaLinea);
        CrearEfectoParticulas(posDestino + Vector3.up * alturaLinea);
    }

    private void CrearEfectoParticulas(Vector3 posicion)
    {
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
    }

    // Método llamado cada turno
    public void ProcesarTurno()
    {
        if (!IsServer) return;

        // Reducir duración
        duracionRestante--;

        // Si es el último turno, limpiar
        if (duracionRestante <= 0)
        {
            LimpiarEfecto();
        }
        else
        {
            // Si no tenemos movimiento automático, intentar mover al menos una vez por turno
            if (!permitirMovimientoAutomatico)
            {
                MoverIngredientesConectados();
            }
        }
    }

    // Método para limpiar el efecto
    public void LimpiarEfecto()
    {
        // Detener el movimiento automático
        if (rutinaMovimientoAutomatico != null)
        {
            StopCoroutine(rutinaMovimientoAutomatico);
            rutinaMovimientoAutomatico = null;
        }

        conexionActiva = false;

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
        // Ocultar línea visual
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
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