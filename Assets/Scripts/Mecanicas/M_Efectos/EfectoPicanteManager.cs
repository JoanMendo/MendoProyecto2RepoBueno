using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EfectoPicanteManager : NetworkBehaviour, IEfectoManager
{
    [Header("Configuración")]
    [SerializeField] private float delayEntreMovimientos = 0.5f;
    [SerializeField] private int movimientosPorTurno = 1;

    // Referencias
    private GameObject nodoOrigen;
    private S_Picante efectoConfigurado;
    private int duracionRestante;

    // Estado de ejecución
    private bool efectoEnEjecucion = false;
    private Coroutine rutinaDeProcesamiento;

    // Para efectos visuales
    private ParticleSystem particulas;

    // Implementación de la interfaz IEfectoManager
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
                efectoEnEjecucion = false;
            }

            // Notificar a los clientes
            LimpiarEfectoClientRpc();

            // Programar destrucción después de un tiempo
            StartCoroutine(DestruirDespuesDeDelay(0.5f));
        }
    }

    // Método principal para iniciar el efecto
    public void IniciarEfecto(GameObject nodo, S_Picante efecto, int duracion)
    {
        // Validaciones
        if (nodo == null)
        {
            Debug.LogError("EfectoPicanteManager: nodo es null");
            return;
        }

        // Guardar referencias
        nodoOrigen = nodo;
        efectoConfigurado = efecto;
        duracionRestante = duracion;

        // Obtener nodo y NodeMap
        Node nodoComp = nodo.GetComponent<Node>();
        if (nodoComp == null)
        {
            Debug.LogError("EfectoPicanteManager: nodo no tiene componente Node");
            return;
        }
        if (efectoConfigurado == null)
        {
            Debug.LogWarning("EfectoPicanteManager: No se pudo encontrar configuración S_Picante");
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

    // Método para realizar movimientos aleatorios
    private void MoverAleatoriamente()
    {
        if (!IsServer) return;
        Debug.Log("Iniciando movimientos aleatorios");
        // Iniciar coroutine solo si no está ya ejecutándose
        if (!efectoEnEjecucion)
        {
            efectoEnEjecucion = true;
            rutinaDeProcesamiento = StartCoroutine(ProcesarMovimientosAleatorios());
        }
    }

    private IEnumerator ProcesarMovimientosAleatorios()
    {
        for (int i = 0; i < movimientosPorTurno; i++)
        {
            // Obtener todos los ingredientes movibles
            List<GameObject> nodosConIngredientes = ObtenerNodosConIngredientesMovibles();

            if (nodosConIngredientes.Count > 0)
            {
                // Elegir un ingrediente al azar para mover
                GameObject nodoAMover = nodosConIngredientes[Random.Range(0, nodosConIngredientes.Count)];
                List<GameObject> posiblesDestinos = ObtenerNodosVacios(nodoAMover);

                if (posiblesDestinos.Count > 0)
                {
                    // Elegir destino aleatorio
                    GameObject nodoDestino = posiblesDestinos[Random.Range(0, posiblesDestinos.Count)];

                    // Ejecutar movimiento
                    MoverIngredienteServerRpc(
                        nodoAMover.GetComponent<NetworkObject>().NetworkObjectId,
                        nodoDestino.GetComponent<NetworkObject>().NetworkObjectId
                    );

                    // Esperar para el próximo movimiento
                    yield return new WaitForSeconds(delayEntreMovimientos);
                }
            }
        }

        efectoEnEjecucion = false;
    }

    // Obtener nodos que contienen ingredientes que se pueden mover
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
    private List<GameObject> ObtenerNodosVacios(GameObject nodoOrigen)
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
    private void MoverIngredienteServerRpc(ulong origenId, ulong destinoId)
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

        // Si el nodo destino ya tiene ingrediente, no hacer nada
        if (nodoDestino.hasIngredient.Value)
        {
            Debug.LogWarning("El nodo destino ya tiene un ingrediente");
            return;
        }

        // Verificar que el nodo origen tenga ingrediente
        if (!nodoOrigen.hasIngredient.Value || nodoOrigen.currentIngredient == null)
        {
            Debug.LogWarning("El nodo origen no tiene ingrediente para mover");
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