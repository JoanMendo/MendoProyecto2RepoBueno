using UnityEngine;
using Unity.Netcode;
/// ‡‡<summary>_PLACEHOLDER‡‡
/// Componente que gestiona efectos temporales aplicados a un nodo.
/// Se adjunta dinámicamente a los nodos cuando reciben efectos temporales.
/// ‡‡</summary>_PLACEHOLDER‡‡
public class EfectoTemporal : NetworkBehaviour
{
    // Referencia al efecto (ScriptableObject)
    public Efectos efecto;

    // Turnos restantes del efecto, sincronizado en red
    public NetworkVariable<int> turnosRestantes = new NetworkVariable<int>();

    // Identificador del efecto para sincronización
    public NetworkVariable<string> efectoID = new NetworkVariable<string>();

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Inicializa el efecto temporal con los datos necesarios
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void Initialize(Efectos efectoSO, int duracion)
    {
        efecto = efectoSO;
        turnosRestantes.Value = duracion;
        efectoID.Value = efectoSO.resourceID;
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Procesa el efecto cada turno y reduce su duración
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void ProcesarTurno()
    {
        if (!IsServer) return;

        if (efecto != null)
        {
            // Ejecutar efecto para este turno
            efecto.EjecutarTurno(gameObject);
        }

        // Reducir duración
        turnosRestantes.Value--;

        // Si la duración llega a 0, finalizar efecto
        if (turnosRestantes.Value <= 0)
        {
            FinalizarEfecto();
        }
    }

    /// ‡‡<summary>_PLACEHOLDER‡‡
    /// Finaliza el efecto y elimina este componente
    /// ‡‡</summary>_PLACEHOLDER‡‡
    public void FinalizarEfecto()
    {
        if (!IsServer) return;

        if (efecto != null)
        {
            efecto.FinalizarEfecto(gameObject);
        }

        // Eliminar este componente después de finalizar
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
    }
}
