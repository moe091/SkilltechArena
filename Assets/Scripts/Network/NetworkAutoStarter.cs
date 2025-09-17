using FishNet;
using UnityEngine;


public class NetworkAutoStarter : MonoBehaviour
{
    [Header("Optional: set to publish to Firebase when hosting")]
    public FirebaseLobbyDirectory firebase;

    [Header("Heartbeat")]
    public float heartbeatSeconds = 10f;

    private bool _published = false;
    private string _lobbyId = null;
    private LobbyRow _row;                    // keep the published row so we can update it
    private Coroutine _heartbeatCo = null;

    void Start()
    {
        Debug.Log("NETWORK AUTOSTART RUNNING!");
        StartCoroutine(BootNextFrame());
    }

    System.Collections.IEnumerator BootNextFrame()
    {
        Debug.Log("[NetworkStarter.BootNextFrame] Returning null");
        yield return null; // wait one frame

        Debug.Log("[NetworkStarter.BootNextFrame] Launching. mode = " + LaunchConfig.NextMode);
        switch (LaunchConfig.NextMode)
        {
            case LaunchConfig.Mode.Host:
                StartHostAndPublish(); // host + publish (single entry point)
                break;

            case LaunchConfig.Mode.Client:
                InstanceFinder.ClientManager.StartConnection(LaunchConfig.Address);
                Debug.Log($"[NetworkAutoStarter] Started as CLIENT connecting to {LaunchConfig.Address}:{LaunchConfig.Port}");
                break;

            case LaunchConfig.Mode.None:
            default:
                Debug.Log("[NetworkAutoStarter] No mode set — did you launch gameplay directly?");
                break;
        }

        LaunchConfig.Clear();
    }

    /// <summary>
    /// Starts FishNet as server + local client, then (optionally) creates one Firebase lobby row.
    /// Also starts a simple heartbeat that updates 'updatedAt'.
    /// </summary>
    async void StartHostAndPublish()
    {
        // Start server + local client.
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();
        Debug.Log("[NetworkAutoStarter] Started as HOST (server + local client).");

        if (firebase == null)
        {
            Debug.Log("[NetworkAutoStarter] Firebase directory not set; hosting without publishing a lobby row.");
            return;
        }

        if (_published)
        {
            Debug.Log("[NetworkAutoStarter] Lobby already published; skipping duplicate publish.");
            return;
        }

        // Build minimal row (CurrentPlayers starts at 1 for host).
        _row = new LobbyRow
        {
            name = LaunchConfig.HostDisplayName,
            map = LaunchConfig.Map,
            cur = 1,
            max = LaunchConfig.MaxPlayers,
            region = LaunchConfig.Region,
            scheme = "udp",
            addr = LaunchConfig.Address,
            port = LaunchConfig.Port,
            updatedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            // Add version/mode here later if your LobbyRow includes them
        };

        try
        {
            _lobbyId = await firebase.CreateAsync(_row);
            _published = true;
            Debug.Log($"[NetworkAutoStarter] Published lobby '{_lobbyId}' ({_row.name}) at {_row.addr}:{_row.port}");

            // Start simple heartbeat
            _heartbeatCo = StartCoroutine(HeartbeatLoop());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkAutoStarter] Failed to publish lobby: {ex.Message}");
        }
    }

    /// <summary>
    /// Every heartbeatSeconds, update only 'updatedAt'.
    /// </summary>
    System.Collections.IEnumerator HeartbeatLoop()
    {
        var wait = new WaitForSecondsRealtime(Mathf.Max(2f, heartbeatSeconds));
        while (_published && !string.IsNullOrEmpty(_lobbyId) && firebase != null)
        {
            _row.updatedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Fire-and-forget; we don't block the loop on network latency.
            _ = firebase.UpdateAsync(_lobbyId, _row);
            yield return wait;
        }
        _heartbeatCo = null;
    }

    void OnDestroy()
    {
        // Stop heartbeat first
        if (_heartbeatCo != null)
        {
            StopCoroutine(_heartbeatCo);
            _heartbeatCo = null;
        }

        // Best-effort delete; fire-and-forget
        if (_published && !string.IsNullOrEmpty(_lobbyId) && firebase != null)
        {
            _ = firebase.DeleteAsync(_lobbyId);
            Debug.Log($"[NetworkAutoStarter] Deleted lobby '{_lobbyId}' on destroy.");
        }
    }
}
