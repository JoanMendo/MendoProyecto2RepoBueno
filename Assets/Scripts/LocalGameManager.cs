using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class LocalGameManager : MonoBehaviour
{
    // Ingredientes (existente)
    public GameObject currentIngredient;
    public ResourcesSO currentIngredientData;

    // Utensilios (existente)
    public Utensilio currentUtensilio;
    private List<GameObject> nodosSeleccionados = new List<GameObject>();

    // Efectos (existente)
    [Header("Efectos")]
    public Efectos currentEfecto;
    private GameObject primerNodoSeleccionado;
    private bool esperandoSegundoNodo = false;

    // Variables para modo enemigo (nuevas)
    [Header("Interacción con Tablero Enemigo")]
    public bool modoSeleccionEnemigo = false;
    private NodeMap tableroEnemigo = null;

    public float actualmoney;
    public static LocalGameManager Instance { get; private set; }

    public void Awake()
    {
        Instance = this;
    }

    // Método para activar el modo selección en tablero enemigo
    public void ActivarModoEnemigo()
    {
        ulong clienteLocal = NetworkManager.Singleton.LocalClientId;

        // Buscar el tablero enemigo
        NodeMap[] todosLosTableros = FindObjectsOfType<NodeMap>();
        foreach (NodeMap tablero in todosLosTableros)
        {
            if (tablero.ownerClientId != clienteLocal)
            {
                tableroEnemigo = tablero;
                break;
            }
        }

        if (tableroEnemigo != null)
        {
            modoSeleccionEnemigo = true;
            Debug.Log($"Modo selección enemigo activado. Tablero enemigo: {tableroEnemigo.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("No se encontró tablero enemigo");
        }
    }

    public void DesactivarModoEnemigo()
    {
        modoSeleccionEnemigo = false;
        tableroEnemigo = null;

        // Limpiar cualquier selección pendiente
        LimpiarSeleccion();

        Debug.Log("Modo selección enemigo desactivado");
    }

    // Método para seleccionar un utensilio (existente, pero actualizado)
    public void SeleccionarUtensilio(Utensilio utensilio)
    {
        LimpiarSeleccion();
        currentUtensilio = utensilio;

        // Podríamos cambiar el cursor o mostrar algún indicador
        CursorManager cursorManager = FindObjectOfType<CursorManager>();
        if (cursorManager != null && utensilio != null && utensilio.prefab3D != null)
        {
            cursorManager.gameObject.GetComponent<MeshRenderer>().material = utensilio.prefab3D.GetComponent<MeshRenderer>().sharedMaterial;
        }

        Debug.Log($"Utensilio seleccionado: {utensilio?.Name ?? "ninguno"}");
    }

    // Método para seleccionar un efecto (nuevo)
    public void SeleccionarEfecto(Efectos efecto)
    {
        // Deseleccionar utensilio si hay alguno
        if (currentUtensilio != null)
        {
            currentUtensilio = null;
        }

        // Cambiar el efecto actual
        currentEfecto = efecto;

        // Cambiar visual del cursor
        CursorManager cursorManager = FindObjectOfType<CursorManager>();
        if (cursorManager != null && efecto != null && efecto.prefab3D != null)
        {
            MeshRenderer cursorRenderer = cursorManager.gameObject.GetComponent<MeshRenderer>();
            MeshRenderer efectoRenderer = efecto.prefab3D.GetComponent<MeshRenderer>();

            if (cursorRenderer != null && efectoRenderer != null && efectoRenderer.sharedMaterial != null)
            {
                cursorRenderer.material = efectoRenderer.sharedMaterial;
            }
        }

        // Resetear el estado de selección
        primerNodoSeleccionado = null;
        esperandoSegundoNodo = false;

        Debug.Log($"Efecto seleccionado: {efecto.Name}");
    }

    // Método que se llama desde ProcesarSeleccionNodoParaUtensilio 
    public void DeseleccionarUtensilio()
    {
        currentUtensilio = null;

        // Restaurar cursor
        CursorManager cursorManager = FindObjectOfType<CursorManager>();
        if (cursorManager != null)
        {
            cursorManager.RestoreOriginalMaterial();
        }
    }

    // Método para deseleccionar efecto
    public void DeseleccionarEfecto()
    {
        currentEfecto = null;
        primerNodoSeleccionado = null;
        esperandoSegundoNodo = false;

        // Restaurar cursor
        CursorManager cursorManager = FindObjectOfType<CursorManager>();
        if (cursorManager != null)
        {
            cursorManager.RestoreOriginalMaterial();
        }
    }

    // Método para procesar selección de nodos en tablero enemigo para efectos (nuevo)
    public void ProcesarSeleccionNodoParaEfectoOponente(GameObject nodoEnemigo)
    {
        if (currentEfecto == null) return;

        // Verificar economía propia (no la del enemigo)
        Economia economia = ObtenerEconomiaJugadorLocal();
        if (economia == null || economia.money.Value < currentEfecto.Price)
        {
            Debug.Log($"No hay suficiente dinero para usar {currentEfecto.Name} en tablero enemigo");
            return;
        }

        // Adaptar lógica para efectos de S_Blanca (que requieren dos nodos)
        if (currentEfecto is S_Blanca)
        {
            if (primerNodoSeleccionado == null)
            {
                // Primer nodo seleccionado
                primerNodoSeleccionado = nodoEnemigo;
                esperandoSegundoNodo = true;

                // Marcar selección visual
                Node nodoComp = nodoEnemigo.GetComponent<Node>();
                if (nodoComp != null)
                {
                    nodoComp.MarcarSeleccion();
                }

                Debug.Log("Seleccione el segundo nodo para crear la conexión en tablero enemigo.");
            }
            else if (esperandoSegundoNodo)
            {
                // Verificar que no se seleccionó el mismo nodo dos veces
                if (primerNodoSeleccionado == nodoEnemigo)
                {
                    Debug.Log("No puede seleccionar el mismo nodo dos veces");
                    return;
                }

                // Segundo nodo seleccionado, programar efecto
                List<GameObject> nodos = new List<GameObject> {
                    primerNodoSeleccionado,
                    nodoEnemigo
                };

                // Intentar programar el efecto
                if (EfectosProgramados.Instance.ProgramarEfecto(currentEfecto, nodos))
                {
                    // Cobrar dinero
                    economia.less_money(currentEfecto.Price);

                    // Desmarcar primer nodo de selección
                    Node primerNodoComp = primerNodoSeleccionado.GetComponent<Node>();
                    if (primerNodoComp != null)
                    {
                        primerNodoComp.DesmarcarSeleccion();
                    }

                    // Resetear estado
                    primerNodoSeleccionado = null;
                    esperandoSegundoNodo = false;

                    // Deseleccionar efecto
                    DeseleccionarEfecto();

                    // Desactivar modo enemigo 
                    DesactivarModoEnemigo();
                }
            }
        }
        else
        {
            // Para efectos de un solo nodo (S_Especial, S_Picante, etc.)
            List<GameObject> nodos = new List<GameObject> { nodoEnemigo };

            if (EfectosProgramados.Instance.ProgramarEfecto(currentEfecto, nodos))
            {
                // Cobrar dinero
                economia.less_money(currentEfecto.Price);

                // Deseleccionar efecto
                DeseleccionarEfecto();

                // Desactivar modo enemigo
                DesactivarModoEnemigo();
            }
        }
    }

    // Método para procesar selección de nodos en tablero enemigo para utensilios (nuevo)
    public void ProcesarSeleccionNodoParaUtensilioOponente(GameObject nodoEnemigo)
    {
        // Verificar que hay un utensilio seleccionado
        if (currentUtensilio == null) return;

        // Verificar economía
        Economia economia = ObtenerEconomiaJugadorLocal();
        if (economia == null || economia.money.Value < currentUtensilio.Price)
        {
            Debug.Log($"No hay suficiente dinero para usar {currentUtensilio.Name} en tablero enemigo");
            return;
        }

        // Verificar que el nodo tenga componente Node
        Node nodoComp = nodoEnemigo.GetComponent<Node>();
        if (nodoComp == null) return;

        // Verificar si el nodo ya está seleccionado
        if (nodosSeleccionados.Contains(nodoEnemigo))
        {
            // Si ya está seleccionado, deseleccionarlo
            nodosSeleccionados.Remove(nodoEnemigo);
            nodoComp.DesmarcarSeleccion();
            return;
        }

        // Si ya tenemos el máximo de nodos, quitar el primero (el más antiguo)
        if (nodosSeleccionados.Count >= currentUtensilio.nodosRequeridos)
        {
            GameObject nodoAntiguo = nodosSeleccionados[0];
            nodosSeleccionados.RemoveAt(0);

            Node nodoAntiguoComp = nodoAntiguo.GetComponent<Node>();
            if (nodoAntiguoComp != null)
            {
                nodoAntiguoComp.DesmarcarSeleccion();
            }
        }

        // Añadir el nuevo nodo
        nodosSeleccionados.Add(nodoEnemigo);
        nodoComp.MarcarSeleccion();

        // Si tenemos todos los nodos necesarios, programar el utensilio
        if (nodosSeleccionados.Count == currentUtensilio.nodosRequeridos)
        {
            // Programar el utensilio
            bool programadoConExito = UtensiliosProgramados.Instance.ProgramarUtensilio(
                currentUtensilio,
                new List<GameObject>(nodosSeleccionados)
            );

            if (programadoConExito)
            {
                // Cobrar el precio si se programa con éxito
                economia.less_money(currentUtensilio.Price);

                // Marcar los nodos como programados
                foreach (GameObject nodoSeleccionado in nodosSeleccionados)
                {
                    Node nodoSelComp = nodoSeleccionado.GetComponent<Node>();
                    if (nodoSelComp != null)
                    {
                        nodoSelComp.MarcarProgramado(currentUtensilio);
                    }
                }

                Debug.Log($"Utensilio {currentUtensilio.Name} programado con éxito en tablero enemigo");

                // Limpiar selección y deseleccionar utensilio
                LimpiarSeleccion();
                DeseleccionarUtensilio();

                // Desactivar modo enemigo
                DesactivarModoEnemigo();
            }
        }
    }

    // Procesar la selección de nodo para un efecto (modificado)
    public void ProcesarSeleccionNodoParaEfecto(GameObject nodoSeleccionado)
    {
        // Verificar si el nodo pertenece al tablero enemigo
        Node nodoComp = nodoSeleccionado.GetComponent<Node>();
        if (nodoComp != null && nodoComp.nodeMap != null &&
            nodoComp.nodeMap.ownerClientId != NetworkManager.Singleton.LocalClientId)
        {
            // Redirigir al método para tablero enemigo
            ProcesarSeleccionNodoParaEfectoOponente(nodoSeleccionado);
            return;
        }

        if (currentEfecto == null) return;

        // Verificar si es S_Blanca que necesita dos nodos
        if (currentEfecto is S_Blanca)
        {
            if (primerNodoSeleccionado == null)
            {
                // Primer nodo seleccionado
                primerNodoSeleccionado = nodoSeleccionado;
                esperandoSegundoNodo = true;

                // Marcar selección visual
                Node nodoSel = nodoSeleccionado.GetComponent<Node>();
                if (nodoSel != null)
                {
                    nodoSel.MarcarSeleccion();
                }

                Debug.Log("Seleccione el segundo nodo para crear la conexión.");
            }
            else if (esperandoSegundoNodo)
            {
                // Verificar si hay suficiente dinero
                Economia economia = ObtenerEconomiaJugadorLocal();
                if (economia == null || economia.money.Value < currentEfecto.Price)
                {
                    Debug.Log($"No hay suficiente dinero para usar {currentEfecto.Name}");

                    // Desmarcar primer nodo
                    Node primerNodoComp = primerNodoSeleccionado.GetComponent<Node>();
                    if (primerNodoComp != null)
                    {
                        primerNodoComp.DesmarcarSeleccion();
                    }

                    primerNodoSeleccionado = null;
                    esperandoSegundoNodo = false;
                    return;
                }

                // Segundo nodo seleccionado, programar efecto
                List<GameObject> nodos = new List<GameObject>
                {
                    primerNodoSeleccionado,
                    nodoSeleccionado
                };

                // Intentar programar el efecto
                if (EfectosProgramados.Instance.ProgramarEfecto(currentEfecto, nodos))
                {
                    // Cobrar dinero
                    economia.less_money(currentEfecto.Price);

                    // Desmarcar primer nodo de selección
                    Node primerNodoComp = primerNodoSeleccionado.GetComponent<Node>();
                    if (primerNodoComp != null)
                    {
                        primerNodoComp.DesmarcarSeleccion();
                    }

                    // Resetear estado
                    primerNodoSeleccionado = null;
                    esperandoSegundoNodo = false;

                    // Deseleccionar efecto
                    DeseleccionarEfecto();
                }
            }
        }
        else
        {
            // Para efectos de un solo nodo (S_Especial, S_Picante, etc.)

            // Verificar si hay suficiente dinero
            Economia economia = ObtenerEconomiaJugadorLocal();
            if (economia == null || economia.money.Value < currentEfecto.Price)
            {
                Debug.Log($"No hay suficiente dinero para usar {currentEfecto.Name}");
                return;
            }

            // Programar el efecto para un solo nodo
            List<GameObject> nodos = new List<GameObject> { nodoSeleccionado };

            if (EfectosProgramados.Instance.ProgramarEfecto(currentEfecto, nodos))
            {
                // Cobrar dinero
                economia.less_money(currentEfecto.Price);

                // Deseleccionar efecto
                DeseleccionarEfecto();
            }
        }
    }

    // Procesar la selección de nodo para utensilio (modificado)
    public void ProcesarSeleccionNodoParaUtensilio(GameObject nodo)
    {
        // Verificar si el nodo pertenece al tablero enemigo
        Node nodoComp = nodo.GetComponent<Node>();
        if (nodoComp != null && nodoComp.nodeMap != null &&
            nodoComp.nodeMap.ownerClientId != NetworkManager.Singleton.LocalClientId)
        {
            // Redirigir al método para tablero enemigo
            ProcesarSeleccionNodoParaUtensilioOponente(nodo);
            return;
        }

        // Verificar si hay un utensilio seleccionado
        if (currentUtensilio == null)
        {
            // Si no hay utensilio pero hay un efecto, derivar al método de efectos
            if (currentEfecto != null)
            {
                ProcesarSeleccionNodoParaEfecto(nodo);
            }
            return;
        }

        // Verificar que el nodo tenga componente Node
        nodoComp = nodo.GetComponent<Node>();
        if (nodoComp == null) return;

        // Verificar si el nodo ya está seleccionado
        if (nodosSeleccionados.Contains(nodo))
        {
            // Si ya está seleccionado, deseleccionarlo
            nodosSeleccionados.Remove(nodo);
            nodoComp.DesmarcarSeleccion();
            return;
        }

        // Si ya tenemos el máximo de nodos, quitar el primero (el más antiguo)
        if (nodosSeleccionados.Count >= currentUtensilio.nodosRequeridos)
        {
            GameObject nodoAntiguo = nodosSeleccionados[0];
            nodosSeleccionados.RemoveAt(0);

            Node nodoAntiguoComp = nodoAntiguo.GetComponent<Node>();
            if (nodoAntiguoComp != null)
            {
                nodoAntiguoComp.DesmarcarSeleccion();
            }
        }

        // Añadir el nuevo nodo
        nodosSeleccionados.Add(nodo);
        nodoComp.MarcarSeleccion();

        // Si tenemos todos los nodos necesarios, programar el utensilio
        if (nodosSeleccionados.Count == currentUtensilio.nodosRequeridos)
        {
            // Verificar si hay suficiente dinero
            Economia economia = ObtenerEconomiaJugadorLocal();
            if (economia == null || economia.money.Value < currentUtensilio.Price)
            {
                Debug.Log($"No hay suficiente dinero para usar {currentUtensilio.Name}");
                return;
            }

            // Programar el utensilio
            bool programadoConExito = UtensiliosProgramados.Instance.ProgramarUtensilio(
                currentUtensilio,
                new List<GameObject>(nodosSeleccionados)
            );

            if (programadoConExito)
            {
                // Cobrar el precio si se programa con éxito
                economia.less_money(currentUtensilio.Price);

                // Marcar los nodos como programados
                foreach (GameObject nodoSeleccionado in nodosSeleccionados)
                {
                    Node nodoSelComp = nodoSeleccionado.GetComponent<Node>();
                    if (nodoSelComp != null)
                    {
                        nodoSelComp.MarcarProgramado(currentUtensilio);
                    }
                }

                Debug.Log($"Utensilio {currentUtensilio.Name} programado con éxito");

                // Limpiar selección y deseleccionar utensilio
                LimpiarSeleccion();
                DeseleccionarUtensilio();
            }
        }
    }

    // Método auxiliar para obtener la economía del jugador local
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

    // Añadir método para limpiar selección
    public void LimpiarSeleccion()
    {
        // Desmarcar nodos seleccionados
        foreach (var nodo in nodosSeleccionados)
        {
            if (nodo != null)
            {
                nodo.GetComponent<Node>().DesmarcarSeleccion();
            }
        }

        nodosSeleccionados.Clear();

        // Desmarcar primer nodo seleccionado si existe
        if (primerNodoSeleccionado != null)
        {
            Node primerNodoComp = primerNodoSeleccionado.GetComponent<Node>();
            if (primerNodoComp != null)
            {
                primerNodoComp.DesmarcarSeleccion();
            }

            primerNodoSeleccionado = null;
            esperandoSegundoNodo = false;
        }

        // Reiniciar cursor si es necesario
        CursorManager cursorManager = FindObjectOfType<CursorManager>();
        if (cursorManager != null)
        {
            // Aquí podrías restablecer el cursor a su estado normal
            cursorManager.RestoreOriginalMaterial();
        }
    }
}