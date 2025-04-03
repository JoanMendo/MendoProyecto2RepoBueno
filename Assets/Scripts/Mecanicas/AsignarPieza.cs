using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsignarPieza : MonoBehaviour
{

    public NodeMap nodeMap;
    public ResourcesSO recurso;
    public int posicion;
    public LayerMask nodosLayer; // Filtrar solo los nodos

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Detectar toque en pantalla
        {
            Debug.Log("se esta enviando el raycast");
            DetectarNodo();
        }
    }

    void DetectarNodo()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, nodosLayer)) // Solo detecta nodos
        {
            if (hit.collider.CompareTag("Nodo"))
            {
                Debug.Log("se esta detctando la posicion");
                posicion = int.Parse(hit.collider.gameObject.name); // Obtener el número del nodo

            }
        }
    }

}
