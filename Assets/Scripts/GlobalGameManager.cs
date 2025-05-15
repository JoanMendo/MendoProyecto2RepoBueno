using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GlobalGameManager : NetworkBehaviour
{
    [Tooltip("Lista de tableros ya presentes en la escena (SceneObjects con NetworkObject).")]
    public List<GameObject> tablerosEnEscena; // Asignar manualmente desde el inspector

    private bool hasAssignedOwners = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer && !hasAssignedOwners)
        {
            AsignarYSpawnearTableros();
            hasAssignedOwners = true;
        }
    }

    private void AsignarYSpawnearTableros()
    {
        if (tablerosEnEscena == null || tablerosEnEscena.Count == 0)
        {
            Debug.LogError("No hay tableros asignados en el inspector");
            return;
        }

        var clientes = new List<ulong>(NetworkManager.Singleton.ConnectedClients.Keys);

        if (clientes.Count < tablerosEnEscena.Count)
        {
            Debug.LogWarning($"Hay más tableros ({tablerosEnEscena.Count}) que clientes conectados ({clientes.Count})");
        }

        for (int i = 0; i < Mathf.Min(clientes.Count, tablerosEnEscena.Count); i++)
        {
            ulong clientId = clientes[i];
            GameObject tablero = tablerosEnEscena[i];

            if (tablero == null)
            {
                Debug.LogError($"Tablero en índice {i} es null");
                continue;
            }

            // Asignar ownership del tablero principal
            AsignarOwnershipRecursivo(tablero, clientId);
        }
    }

    private void AsignarOwnershipRecursivo(GameObject parent, ulong clientId)
    {
        NetworkObject netObj = parent.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogWarning($"Objeto {parent.name} no tiene NetworkObject");
            return;
        }

        // Spawnear y asignar ownership si no está ya spawned
        if (!netObj.IsSpawned)
        {
            netObj.SpawnWithOwnership(clientId);
        }
        else
        {
            netObj.ChangeOwnership(clientId);
        }

        // Procesar NodeMap si existe
        NodeMap nodeMap = parent.GetComponentInChildren<NodeMap>();
        if (nodeMap != null)
        {
            nodeMap.Generate3DTilemap(); // Asegurarse que los nodos están generados

            foreach (GameObject casilla in nodeMap.nodesList)
            {
                if (casilla == null) continue;

                NetworkObject childNetObj = casilla.GetComponent<NetworkObject>();
                if (childNetObj != null)
                {
                    if (!childNetObj.IsSpawned)
                    {
                        childNetObj.SpawnWithOwnership(clientId);
                    }
                    else
                    {
                        childNetObj.ChangeOwnership(clientId);
                    }
                }
            }
        }

        // Asignar ownership a todos los hijos con NetworkObject
        foreach (Transform child in parent.transform)
        {
            AsignarOwnershipRecursivo(child.gameObject, clientId);
        }
    }
}