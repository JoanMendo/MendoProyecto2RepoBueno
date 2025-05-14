
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Efecto", menuName = "CookingGame/Resources/Efecto")]
public class Efectos : ResourcesSO
{
    [Header("Propiedades de Efecto")]
    [Tooltip("Duraci�n del efecto en turnos (-1 para permanente)")]
    public int duracion = 1;

    [Tooltip("Tipo de efecto (buff, debuff, transformaci�n, etc.)")]
    public string tipoEfecto;

    [Tooltip("Si el efecto se activa inmediatamente o al final del turno")]
    public bool activacionInmediata = true;

    // Implementaci�n de la interfaz IEfecto
    public string Nombre => Name;
    public int Duracion => duracion;
    public bool EsActivacionInmediata => activacionInmediata;

    public virtual void Activar(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Implementaci�n existente
        ActivarEfecto(nodoObjetivo, mapa);
    }

    public virtual void ProcesarTurno(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Por defecto, llamamos al m�todo existente
        EjecutarTurno(nodoObjetivo);
    }

    public virtual void Finalizar(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Por defecto, llamamos al m�todo existente
        FinalizarEfecto(nodoObjetivo);
    }

    public virtual List<GameObject> CalcularNodosAfectados(GameObject nodoObjetivo, NodeMap mapa)
    {
        // Implementaci�n simple que solo devuelve el nodo objetivo
        if (nodoObjetivo == null)
            return new List<GameObject>();

        return new List<GameObject>() { nodoObjetivo };
    }

    // M�todos existentes que ahora redirigen a la interfaz
    public override void ActivarEfecto(GameObject nodoOrigen, NodeMap nodeMap)
    {
        // Calcular nodos afectados
        List<GameObject> nodosAfectados = CalcularNodosAfectados(nodoOrigen, nodeMap);

        // Aplicar efecto espec�fico
        AplicarEfectoEspecifico(nodoOrigen, nodosAfectados);

        // Log para debugging
        Debug.Log($"Efecto {Name} activado desde {nodoOrigen.name} afectando a {nodosAfectados.Count} nodos por {duracion} turnos");
    }

    protected virtual void AplicarEfectoEspecifico(GameObject nodoOrigen, List<GameObject> nodosAfectados)
    {
        // Implementaci�n base vac�a - clases hijas sobreescriben seg�n necesidad
    }

    public virtual void EjecutarTurno(GameObject nodoAfectado)
    {
        // Por defecto no hace nada - clases derivadas implementan efectos por turno
        Debug.Log($"Efecto {Name} en ejecuci�n sobre {nodoAfectado.name}");
    }

    public virtual void FinalizarEfecto(GameObject nodoAfectado)
    {
        Debug.Log($"Efecto {Name} finalizado en {nodoAfectado.name}");
    }
}