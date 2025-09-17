using System;
using System.Collections;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    private NetworkManager _nm;

    private void Awake()
    {
        _nm = InstanceFinder.NetworkManager;
    }

    private void OnEnable()
    {
        if (_nm == null) return;

        // Spawn existing connections after the scene is live.
        // Delay one frame to ensure the networked scene is fully initialized.
        if (_nm.IsServer)
            SpawnExisting();

        // Also handle players who connect later (clients joining your host/dedicated server)
        _nm.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            SpawnFor(conn);
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            // cleanup for 'conn'
        }
    }

    private void OnDisable()
    {
        if (_nm != null)
            _nm.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
    }

    private void SpawnExisting()
    {
        foreach (var kvp in _nm.ServerManager.Clients) // Iterate over KeyValuePair
        {
            NetworkConnection conn = kvp.Value; // Extract the NetworkConnection from the KeyValuePair
            SpawnFor(conn);
        }
    }

     
    private void SpawnFor(NetworkConnection conn)
    {
        if (conn == null || !conn.IsActive) return;
        if (playerPrefab == null) { Debug.LogError("[POCSpawner] No playerPrefab assigned!"); return; }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : new Vector3(0f, 1f, 0f);
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        NetworkObject no = Instantiate(playerPrefab, pos, rot);
        _nm.ServerManager.Spawn(no, conn); // gives ownership to that client
        Debug.Log($"[SERVER] Spawned player for conn {conn.ClientId} at {pos}");
    }
}

