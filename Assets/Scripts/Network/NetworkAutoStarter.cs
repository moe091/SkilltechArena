using FishNet;
using UnityEngine;

public class NetworkAutoStarter : MonoBehaviour
{
    [Header("Optional (only used when hosting)")]
    public FirebaseLobbyDirectory firebase;   // drag your Firebase GO here if you want auto-publish

    void Start()
    {
        // Defer one frame so FishNet's NetworkManager is ready.
        StartCoroutine(BootNextFrame());
    }

    System.Collections.IEnumerator BootNextFrame()
    {
        yield return null;

        switch (LaunchConfig.NextMode)
        {
            case LaunchConfig.Mode.Host:
                StartHostAndMaybePublish();
                break;

            case LaunchConfig.Mode.Client:
                StartClient();
                break;

            case LaunchConfig.Mode.None:
            default:
                Debug.Log("[NetworkAutoStarter] No mode set. (Did you enter the scene directly?)");
                break;
        }

        // Clear so re-entering scene doesn’t repeat an old command
        LaunchConfig.Clear();
    }

    async void StartHostAndMaybePublish()
    {
        // Start server + local client (listen server)
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();

        Debug.Log("[NetworkAutoStarter] Started HOST (server+client).");

        if (firebase != null)
        {
            // Build a row and publish to Firebase
            var row = new LobbyRow
            {
                name = LaunchConfig.HostDisplayName,
                map = "Default",
                cur = 1,
                max = LaunchConfig.MaxPlayers,
                region = LaunchConfig.Region,
                scheme = "udp",
                addr = "127.0.0.1",       // replace with your LAN/public IP later if needed
                port = LaunchConfig.Port
            };

            var id = await firebase.CreateAsync(row);
            firebase.BeginHeartbeat(id, row);

            // Optional: keep 'cur' in sync when players join/leave
            InstanceFinder.ServerManager.OnRemoteConnectionState += (conn, state, asServer) =>
            {
                if (!asServer) return;
                if (state == FishNet.Transporting.RemoteConnectionState.Started) { row.cur++; _ = firebase.UpdateAsync(id, row); }
                else if (state == FishNet.Transporting.RemoteConnectionState.Stopped) { row.cur = Mathf.Max(1, row.cur - 1); _ = firebase.UpdateAsync(id, row); }
            };

            Debug.Log($"[NetworkAutoStarter] Published lobby {id} to Firebase.");
        }
    }

    void StartClient()
    {
        // Uses address from LaunchConfig; port comes from your transport’s Inspector settings
        InstanceFinder.ClientManager.StartConnection(LaunchConfig.Address);
        Debug.Log($"[NetworkAutoStarter] Started CLIENT to {LaunchConfig.Address}:{LaunchConfig.Port}");
    }
}
