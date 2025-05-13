using UnityEngine;
using Unity.Netcode;

/// ��<summary>_PLACEHOLDER��
/// Representa un bot�n de "Listo" que puede ser activado por el jugador que tiene autoridad.
/// Comunica su estado al TurnManager y cambia de color visualmente.
/// ��</summary>_PLACEHOLDER��
public class ReadyButton : NetworkBehaviour, IInteractuable
{
    [Header("Referencias")]
    [Tooltip("Referencia al renderer que cambiar� de color")]
    [SerializeField] private Renderer buttonRenderer;

    [Header("Colores de Estado")]
    [Tooltip("Color cuando el jugador est� listo")]
    [SerializeField] private Color readyColor = Color.green;
    [Tooltip("Color cuando el jugador no est� listo")]
    [SerializeField] private Color notReadyColor = Color.red;

    /// ����<summary>_PLACEHOLDER��_PLACEHOLDER��
    /// Estado de listo/no listo del jugador, sincronizado en red
    /// ����</summary>_PLACEHOLDER��_PLACEHOLDER��
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false);

    /// ����<summary>_PLACEHOLDER��_PLACEHOLDER��
    /// Color actual del bot�n, sincronizado en red
    /// ����</summary>_PLACEHOLDER��_PLACEHOLDER��
    private NetworkVariable<Color> buttonColor = new NetworkVariable<Color>();

    /// ����<summary>_PLACEHOLDER��_PLACEHOLDER��
    /// Referencia al TurnManager para comunicar cambios de estado
    /// ����</summary>_PLACEHOLDER��_PLACEHOLDER��
    private TurnManager turnManager;

    [HideInInspector] public ulong AssociatedTableroId;

    private void Awake()
    {
        Debug.Log($"[ReadyButton] Awake - Button ID: {GetInstanceID()}");

        // Encontrar el TurnManager al inicio
        turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager == null)
        {
            Debug.LogError("[ReadyButton] No se encontr� TurnManager en la escena");
        }
        else
        {
            Debug.Log("[ReadyButton] TurnManager encontrado correctamente");
        }

        // Suscribirse a los cambios de estado
        isReady.OnValueChanged += OnReadyChanged;
        buttonColor.OnValueChanged += OnColorChanged;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[ReadyButton] OnNetworkSpawn - OwnerClientId: {OwnerClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}");

        // Inicializar color seg�n estado inicial
        UpdateButtonColor(isReady.Value);

        if (buttonRenderer == null)
        {
            Debug.LogError("[ReadyButton] �No hay buttonRenderer asignado!");
        }
        else
        {
            Debug.Log($"[ReadyButton] buttonRenderer encontrado: {buttonRenderer.name}");
        }
    }

    private void OnDisable()
    {
        Debug.Log("[ReadyButton] OnDisable");

        // Desuscribirse de eventos cuando se desactiva
        if (isReady != null)
            isReady.OnValueChanged -= OnReadyChanged;

        if (buttonColor != null)
            buttonColor.OnValueChanged -= OnColorChanged;
    }

    /// ����<summary>_PLACEHOLDER��_PLACEHOLDER��
    /// M�todo llamado cuando el jugador interact�a con el bot�n
    /// ����</summary>_PLACEHOLDER��_PLACEHOLDER��
    public void Interactuar()
    {
        Debug.Log($"[ReadyButton] Interactuar - IsOwner: {IsOwner}, Estado actual: {isReady.Value}");

        // Solo el due�o del objeto puede cambiar su estado
        if (!IsOwner)
        {
            Debug.LogWarning("[ReadyButton] Interactuar fallido - No es el propietario del objeto");
            return;
        }

        // Cambiar estado (funciona en el servidor y se sincroniza)
        bool newState = !isReady.Value;
        Debug.Log($"[ReadyButton] Solicitando cambio de estado a: {newState}");
        UpdateReadyStateServerRpc(newState);
    }

    /// ����<summary>_PLACEHOLDER��_PLACEHOLDER��
    /// RPC para cambiar el estado de listo en el servidor
    /// ����</summary>_PLACEHOLDER��_PLACEHOLDER��
    [ServerRpc(RequireOwnership = true)]
    private void UpdateReadyStateServerRpc(bool newState)
    {
        Debug.Log($"[ReadyButton] UpdateReadyStateServerRpc - Nuevo estado: {newState}");

        // Simplemente actualiza el estado y el color
        isReady.Value = newState;
        UpdateButtonColor(newState);

        
        // En su lugar, notificar que este bot�n ha cambiado
        NotifyReadyButtonChangedServerRpc(OwnerClientId, newState);
    }

    // Nuevo RPC para notificar al servidor del cambio
    [ServerRpc]
    private void NotifyReadyButtonChangedServerRpc(ulong clientId, bool isReady)
    {
        Debug.Log($"[ReadyButton] Notificando cambio de estado para cliente {clientId}: {isReady}");
        // No hacemos nada m�s aqu� - el servidor gestionar� esto por su cuenta
    }

    /// ����<summary>_PLACEHOLDER��_PLACEHOLDER��
    /// Actualiza el color del bot�n seg�n su estado
    /// ����</summary>_PLACEHOLDER��_PLACEHOLDER��
    private void UpdateButtonColor(bool ready)
    {
        Debug.Log($"[ReadyButton] UpdateButtonColor - Estado: {ready}");
        buttonColor.Value = ready ? readyColor : notReadyColor;
    }

    /// ����<summary>_PLACEHOLDER��_PLACEHOLDER��
    /// Responde a cambios en el estado de listo
    /// ����</summary>_PLACEHOLDER��_PLACEHOLDER��
    private void OnReadyChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[ReadyButton] OnReadyChanged - Anterior: {oldValue}, Nuevo: {newValue}");

        // Si estamos en el servidor, aseguramos que el color sea actualizado
        if (IsServer)
        {
            UpdateButtonColor(newValue);
        }
    }

    /// ����<summary>_PLACEHOLDER��_PLACEHOLDER��
    /// Responde a cambios en el color del bot�n, actualizando el renderer
    /// ����</summary>_PLACEHOLDER��_PLACEHOLDER��
    private void OnColorChanged(Color oldValue, Color newValue)
    {
        Debug.Log($"[ReadyButton] OnColorChanged - Anterior: {oldValue}, Nuevo: {newValue}");

        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = newValue;
            Debug.Log($"[ReadyButton] Color actualizado en el renderer a: {newValue}");
        }
        else
        {
            Debug.LogError("[ReadyButton] No se puede actualizar color - buttonRenderer es null");
        }
    }

    /// ����<summary>_PLACEHOLDER��_PLACEHOLDER��
    /// M�todo p�blico para reiniciar el estado del bot�n (llamado desde TurnManager)
    /// ����</summary>_PLACEHOLDER��_PLACEHOLDER��
    public void ResetButtonState()
    {
        Debug.Log("[ReadyButton] ResetButtonState llamado");

        // Solo el servidor puede reiniciar estados
        if (!IsServer)
        {
            Debug.LogWarning("[ReadyButton] ResetButtonState fallido - No es el servidor");
            return;
        }

        isReady.Value = false;
        UpdateButtonColor(false);
        Debug.Log("[ReadyButton] Estado reiniciado correctamente");
    }
}