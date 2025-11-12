using FishNet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyCreationHelpers : MonoBehaviour
{

    public static float heartbeatSeconds = 10f;
    public static string lobbyId;
    public static FirebaseLobbyDirectory firebase;
    public static LobbyRow row;

    private static bool _published = false;
    private static Coroutine _heartbeatCo = null;


    public static async void CreateLobbyAndStartServer(ServerLobbyConfig cfg, FirebaseLobbyDirectory lobbyDir, bool asHost)
    {
        firebase = lobbyDir;
        Debug.Log("[LobbyCreationHelpers.CreateLobbyAndStartServer] Creating lobby, asHost=" + asHost);
        InstanceFinder.ServerManager.StartConnection();
        if (asHost)
        {
            InstanceFinder.ClientManager.StartConnection();
        }

        row = new LobbyRow
        {
            name = cfg.lobbyName,
            map = cfg.map,
            cur = 0,
            max = cfg.maxPlayers,
            region = cfg.region,
            scheme = "udp",
            addr = cfg.publicIP,
            port = cfg.port,
            updatedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        try
        {
            lobbyId = await firebase.CreateAsync(row);
            _published = true;
            Debug.Log($"[NetworkAutoStarter] Published lobby '{lobbyId}' ({row.name}) at {row.addr}:{row.port}");

            // Start simple heartbeat
            _heartbeatCo = lobbyDir.StartCoroutine(HeartbeatLoop());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkAutoStarter] Failed to publish lobby: {ex.Message}");
        }


    }

    static System.Collections.IEnumerator HeartbeatLoop()
    {
        var wait = new WaitForSecondsRealtime(Mathf.Max(2f, heartbeatSeconds));
        while (_published && !string.IsNullOrEmpty(lobbyId) && firebase != null)
        {
            row.updatedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Fire-and-forget; we don't block the loop on network latency.
            _ = firebase.UpdateAsync(lobbyId, row);
            yield return wait;
        }
        _heartbeatCo = null;
    }
}
