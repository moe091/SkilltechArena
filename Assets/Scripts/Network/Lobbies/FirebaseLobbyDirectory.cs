using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

[Serializable]
public class LobbyRow
{
    public string id;           // set client-side when reading/creating
    public string name;         // host label
    public string map;          // optional
    public int cur;          // current players
    public int max;          // cap
    public string region;       // hint string
    public string scheme;       // "udp" for this prototype
    public string addr;         // host IP/hostname
    public int port;         // host port
    public long updatedAt;    // unix ms; we heartbeat this
}

public class FirebaseLobbyDirectory : MonoBehaviour
{
    [Header("Firebase")]
    [Tooltip("Your RTDB root, like https://<project>-default-rtdb.firebaseio.com")]
    public string databaseUrl;
    [Tooltip("Path where lobbies live")]
    public string tablePath = "/lobbies";

    [Header("Heartbeat")]
    public float heartbeatSeconds = 10f;

    string _myLobbyId;
    bool _heartbeatRunning;

    string Base(string path) => $"{databaseUrl.TrimEnd('/')}{path}.json";

    // Create a row; returns Firebase key (e.g., "-NvAbCdEf")
    public async System.Threading.Tasks.Task<string> CreateAsync(LobbyRow row)
    {
        row.updatedAt = Now();
        using var req = new UnityWebRequest(Base(tablePath), UnityWebRequest.kHttpVerbPOST);
        var bytes = new UTF8Encoding().GetBytes(JsonUtility.ToJson(row));
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        var op = req.SendWebRequest();
        while (!op.isDone) await System.Threading.Tasks.Task.Yield();
        if (req.result != UnityWebRequest.Result.Success) throw new Exception(req.error);

        // Firebase returns {"name":"-Key"}
        var s = req.downloadHandler.text;
        int a = s.IndexOf("\"name\":\""); if (a < 0) return null;
        a += 8; int b = s.IndexOf("\"", a); return s.Substring(a, b - a);
    }

    public async System.Threading.Tasks.Task UpdateAsync(string id, LobbyRow row)
    {
        row.updatedAt = Now();
        var req = new UnityWebRequest(Base($"{tablePath}/{id}"), "PATCH");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(row)));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        var op = req.SendWebRequest();
        while (!op.isDone) await System.Threading.Tasks.Task.Yield();
        if (req.result != UnityWebRequest.Result.Success) throw new Exception(req.error);
    }

    public async System.Threading.Tasks.Task DeleteAsync(string id)
    {
        using var req = UnityWebRequest.Delete(Base($"{tablePath}/{id}"));
        var op = req.SendWebRequest();
        while (!op.isDone) await System.Threading.Tasks.Task.Yield();
    }

    public async System.Threading.Tasks.Task<int> DeleteExpiredAsync(System.Collections.Generic.IEnumerable<string> ids)
    {
        var arr = ids?.Distinct().ToArray() ?? System.Array.Empty<string>();
        if (arr.Length == 0) return 0;

        // Build {"id1":null,"id2":null}
        var sb = new StringBuilder();
        sb.Append('{');
        for (int i = 0; i < arr.Length; i++)
        {
            if (i > 0) sb.Append(',');
            // Basic JSON string escape for quotes/backslashes if your keys can contain them (Firebase push IDs don't)
            sb.Append('"').Append(arr[i]).Append("\":null");
        }
        sb.Append('}');
        var body = Encoding.UTF8.GetBytes(sb.ToString());

        // PATCH to the collection path (e.g., "lobbies.json")
        using var req = new UnityWebRequest(Base(tablePath), "PATCH");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        var op = req.SendWebRequest();
        while (!op.isDone) await System.Threading.Tasks.Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
            throw new System.Exception(req.error);

        return arr.Length;
    }

    // Returns a map of firebaseKey -> LobbyRow
    public async System.Threading.Tasks.Task<Dictionary<string, LobbyRow>> ListAsync()
    {
        using var req = UnityWebRequest.Get(Base(tablePath));
        req.downloadHandler = new DownloadHandlerBuffer();
        var op = req.SendWebRequest();
        while (!op.isDone) await System.Threading.Tasks.Task.Yield();
        if (req.result != UnityWebRequest.Result.Success) throw new Exception(req.error);

        var json = req.downloadHandler.text;
        if (string.IsNullOrEmpty(json) || json == "null")
            return new Dictionary<string, LobbyRow>();

        var dict = MiniJson.Deserialize(json) as Dictionary<string, object>;
        var result = new Dictionary<string, LobbyRow>();
        foreach (var kv in dict)
        {
            var rowJson = MiniJson.Serialize(kv.Value);
            var row = JsonUtility.FromJson<LobbyRow>(rowJson);
            row.id = kv.Key;
            result[row.id] = row;
        }
        return result;
    }

    // Starts a periodic Update on this row to keep it fresh
    public void BeginHeartbeat(string lobbyId, LobbyRow row)
    {
        _myLobbyId = lobbyId;
        if (!_heartbeatRunning) StartCoroutine(Heartbeat(row));
    }

    System.Collections.IEnumerator Heartbeat(LobbyRow row)
    {
        _heartbeatRunning = true;
        var wait = new WaitForSecondsRealtime(heartbeatSeconds);
        while (!string.IsNullOrEmpty(_myLobbyId))
        {
            row.updatedAt = Now();
            _ = UpdateAsync(_myLobbyId, row); // fire-and-forget
            yield return wait;
        }
        _heartbeatRunning = false;
    }

    void OnApplicationQuit()
    {
        if (!string.IsNullOrEmpty(_myLobbyId))
            _ = DeleteAsync(_myLobbyId);
        _myLobbyId = null;
    }

    static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
