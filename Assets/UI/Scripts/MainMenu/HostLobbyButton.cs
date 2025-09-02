using FishNet;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostLobbyButton : MonoBehaviour
{
    public FirebaseLobbyDirectory directory;
    public string gameplaySceneName = "SampleScene";
    public string hostDisplayName = "Host";
    public string region = "NA";
    public int maxPlayers = 8;
    public int port = 7777;
    public string hostAddressOverride;

    string _lobbyId;
    LobbyRow _row;

    enum Pending { None, StartHost }
    Pending _pending;

    public void OnClickHost()
    {
        _pending = Pending.StartHost;
        SceneManager.LoadScene(gameplaySceneName);
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (_pending == Pending.StartHost && s.name == gameplaySceneName)
        {
            _pending = Pending.None;
            StartHostAndPublish();
        }
    }

    async void StartHostAndPublish()
    {
        // Wait one frame so the NetworkManager is ready
        await System.Threading.Tasks.Task.Yield();

        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();

        string addr = !string.IsNullOrEmpty(hostAddressOverride) ? hostAddressOverride : "127.0.0.1";
        _row = new LobbyRow
        {
            name = hostDisplayName,
            map = "Default",
            cur = 1,
            max = maxPlayers,
            region = region,
            scheme = "udp",
            addr = addr,
            port = port
        };

        _lobbyId = await directory.CreateAsync(_row);
        directory.BeginHeartbeat(_lobbyId, _row);

        Debug.Log($"[HostLobbyButton] Hosting on {addr}:{port}, published {_lobbyId}");
    }

    void OnApplicationQuit()
    {
        if (!string.IsNullOrEmpty(_lobbyId)) _ = directory.DeleteAsync(_lobbyId);
    }
}
