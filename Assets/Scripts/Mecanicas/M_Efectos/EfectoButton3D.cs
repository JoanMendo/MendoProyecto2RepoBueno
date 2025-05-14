using UnityEngine;
using Unity.Netcode;

public class EfectoButton3D : MonoBehaviour, IInteractuable
{
    [Header("Configuración")]
    [SerializeField] private string nombreEfecto;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material materialNormal;
    [SerializeField] private Material materialSeleccionado;
    [SerializeField] private Material materialNoDisponible;

    // Referencia al efecto
    private Efectos efectoData;

    // Visual para indicador de precio
    [SerializeField] private TextMesh textoPrecio;

    private void Awake()
    {
        // Establecer el layer correcto para la interacción
        gameObject.layer = LayerMask.NameToLayer("Interact");

        // Asegurar que tenemos las referencias
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        // Guardar el material normal si no está asignado
        if (materialNormal == null && meshRenderer != null)
            materialNormal = meshRenderer.material;
    }

    private void Start()
    {
        Debug.Log($"EfectoButton3D iniciado");
        // Cargar datos del efecto
        CargarDatosEfecto();

        // Actualizar visuales
        ActualizarEstadoVisual();
    }

    private void CargarDatosEfecto()
    {
        if (string.IsNullOrEmpty(nombreEfecto)) return;

        if (EfectosManager.Instance != null)
        {
            efectoData = EfectosManager.Instance.GetEfectoPorNombre(nombreEfecto);

            if (efectoData != null)
            {
                // Actualizar el mesh si es posible
                if (efectoData.prefab3D != null)
                {
                    MeshFilter meshSource = efectoData.prefab3D.GetComponent<MeshFilter>();
                    if (meshSource != null)
                    {
                        MeshFilter myMesh = GetComponent<MeshFilter>();
                        if (myMesh != null)
                        {
                            myMesh.mesh = meshSource.sharedMesh;
                        }
                    }
                }

                // Actualizar texto de precio
                if (textoPrecio != null)
                {
                    textoPrecio.text = efectoData.Price.ToString();
                }
            }
        }
    }

    // MÉTODO AUXILIAR: Obtener la economía del jugador local
    private Economia ObtenerEconomiaJugadorLocal()
    {
        // Obtener el ID de cliente local
        ulong clienteLocalId = NetworkManager.Singleton.LocalClientId;

        // Buscar el NodeMap que pertenece a este cliente
        NodeMap[] todosLosTableros = FindObjectsOfType<NodeMap>();
        foreach (NodeMap tablero in todosLosTableros)
        {
            if (tablero.ownerClientId == clienteLocalId)
            {
                // Devolver la economía asociada a este tablero
                return tablero.economia;
            }
        }

        Debug.LogWarning("No se encontró un tablero para el jugador local");
        return null;
    }

    public void Interactuar()
    {
        Debug.Log($"Botón de efecto {nombreEfecto} interactuado");

        if (efectoData == null)
        {
            Debug.LogWarning($"No se encontraron datos para el efecto: {nombreEfecto}");
            return;
        }

        // Obtener la economía del jugador local
        Economia economiaLocal = ObtenerEconomiaJugadorLocal();
        if (economiaLocal == null)
        {
            Debug.LogError("No se pudo obtener la economía del jugador local");
            return;
        }

        // Verificar si tenemos suficiente dinero
        bool tieneSuficienteDinero = economiaLocal.money.Value >= efectoData.Price;

        if (!tieneSuficienteDinero)
        {
            Debug.Log($"Dinero insuficiente para {nombreEfecto}: " +
                     $"necesitas {efectoData.Price}, tienes {economiaLocal.money.Value}");
            return;
        }

        // Seleccionar el efecto
        if (LocalGameManager.Instance != null)
        {
            LocalGameManager.Instance.SeleccionarEfecto(efectoData);

            // Actualizar todos los botones
            EfectoButton3D[] todosBotones = FindObjectsOfType<EfectoButton3D>();
            foreach (var boton in todosBotones)
            {
                boton.ActualizarEstadoVisual();
            }
        }
    }

    public void ActualizarEstadoVisual()
    {
        if (meshRenderer == null || efectoData == null) return;

        // Verificar si está seleccionado
        bool seleccionado = false;
        if (LocalGameManager.Instance != null &&
            LocalGameManager.Instance.currentEfecto == efectoData)
        {
            seleccionado = true;
        }

        // Obtener la economía del jugador local para verificar dinero disponible
        Economia economiaLocal = ObtenerEconomiaJugadorLocal();
        bool tieneSuficienteDinero = true;

        if (economiaLocal != null)
        {
            tieneSuficienteDinero = economiaLocal.money.Value >= efectoData.Price;
        }

        // Asignar material según estado
        if (seleccionado && materialSeleccionado != null)
        {
            meshRenderer.material = materialSeleccionado;
        }
        else if (!tieneSuficienteDinero && materialNoDisponible != null)
        {
            meshRenderer.material = materialNoDisponible;
        }
        else
        {
            meshRenderer.material = materialNormal;
        }
    }

    private void OnEnable()
    {
        // Actualizar al activar
        ActualizarEstadoVisual();
    }
}
