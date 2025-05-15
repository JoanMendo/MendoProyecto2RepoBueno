using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerBoardManager : NetworkBehaviour
{
    // Dictionary that maps clientIds to their NodeMaps
    private static Dictionary<ulong, NodeMap> playerBoards = new Dictionary<ulong, NodeMap>();

    // Register a player's board
    public static void RegisterBoard(ulong clientId, NodeMap board)
    {
        playerBoards[clientId] = board;
    }

    // Get an opponent's board
    public static NodeMap GetOpponentBoard(ulong myClientId)
    {
        foreach (var kvp in playerBoards)
        {
            if (kvp.Key != myClientId)
            {
                return kvp.Value;
            }
        }
        return null;
    }
}
