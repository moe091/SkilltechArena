using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FishNet;

public class LobbyBrowser : MonoBehaviour
{
    [Header("Data")]
    public FirebaseLobbyDirectory directory;     // drag your Firebase GO here
    public int freshnessMs = 30000;              // only show rows updated within this window

    [Header("UI")]
    public Transform lobbyListRoot;              // ScrollView/Viewport/Content
    public GameObject lobbyListItemPrefab;       // must have Text children: Name, Players, Region, Map, Mode, and a Button named Join
    public Button refreshButton;                 // your Refresh button

    [Header("Scenes")]
    public string gameplaySceneName = "Game";

    void Awake()
    {
        if (refreshButton) refreshButton.onClick.AddListener(OnClickRefresh);
    }


    public async void OnClickRefresh()
    {
        Debug.Log("Refresh Clicked!");
        foreach (Transform c in lobbyListRoot) Destroy(c.gameObject);

        // Fetch rows
        var all = await directory.ListAsync();
        Debug.Log("Got Lobby List: " + all.Count);

        var now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // NEW: opportunistic prune — anything older than freshnessMs
        var staleIds = all
            .Where(kv => now - kv.Value.updatedAt > freshnessMs)
            .Select(kv => kv.Key)
            .ToList();

        if (staleIds.Count > 0)
        {
            try
            {
                int deleted = await directory.DeleteExpiredAsync(staleIds);
                Debug.Log($"Pruned {deleted} stale lobby(s).");
                // Optionally remove them from 'all' so they don't get considered below
                foreach (var id in staleIds) all.Remove(id);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Prune failed for {staleIds.Count} IDs: {ex.Message}");
            }
        }

        // Filter fresh, then sort (newest first, then by players)
        var rows = all.Values
                      .Where(r => now - r.updatedAt <= freshnessMs)
                      .OrderByDescending(r => r.updatedAt)
                      .ThenByDescending(r => r.cur)
                      .ToList();

        // Populate UI
        foreach (var r in rows)
        {
            var go = Instantiate(lobbyListItemPrefab, lobbyListRoot);
            var nameTxt = go.transform.Find("Name")?.GetComponent<Text>();
            var playersTxt = go.transform.Find("Players")?.GetComponent<Text>();
            var regionTxt = go.transform.Find("Region")?.GetComponent<Text>();
            var mapTxt = go.transform.Find("Map")?.GetComponent<Text>();
            var modeTxt = go.transform.Find("Mode")?.GetComponent<Text>();
            var joinBtn = go.transform.Find("Join")?.GetComponent<Button>();

            if (nameTxt) nameTxt.text = string.IsNullOrWhiteSpace(r.name) ? "Lobby" : r.name;
            if (playersTxt) playersTxt.text = $"{r.cur}/{r.max}";
            if (regionTxt) regionTxt.text = r.region ?? "";
            if (mapTxt) mapTxt.text = r.map ?? "";
            if (modeTxt) modeTxt.text = LaunchConfig.GameMode;

            if (joinBtn)
            {
                Debug.Log("joinBtn exists, trying to join!");
                joinBtn.onClick.AddListener(() => Join(r));
            } else
            {
                Debug.Log("joinBtn doesn't exist!");
            }
        }
    }


    void Join(LobbyRow r)
    {
        Debug.Log("JOIN CLICKED");
        var nm = InstanceFinder.NetworkManager;
        if (nm != null)
        {
            if (nm.ClientManager.Started) nm.ClientManager.StopConnection();
            if (nm.ServerManager.Started) nm.ServerManager.StopConnection(true);
        }


        // Set launch mode for the gameplay scene auto-starter
        LaunchConfig.NextMode = LaunchConfig.Mode.Client;
        LaunchConfig.Address = r.addr;       // comes from the host’s published row
        LaunchConfig.Port = r.port;

        ConnectionWatchdog.Begin(timeoutSeconds: 4f, menuScene: "MainMenu");

        SceneManager.LoadScene(gameplaySceneName);
    }
}
