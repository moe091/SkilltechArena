using System.Diagnostics;
using UnityEngine;

public class SanityTest : MonoBehaviour
{
    public FirebaseLobbyDirectory dir;

    async void Start()
    {
        if (!dir) dir = GetComponent<FirebaseLobbyDirectory>();

        var row = new LobbyRow
        {
            name = "TestLobby",
            map = "Demo",
            cur = 1,
            max = 8,
            region = "NA",
            scheme = "udp",
            addr = "127.0.0.1",
            port = 7777
        };

        var id = await dir.CreateAsync(row);
        UnityEngine.Debug.Log("Created lobby id: " + id);

        var all = await dir.ListAsync();
        UnityEngine.Debug.Log("Lobby count: " + all.Count);

        await dir.DeleteAsync(id);
        UnityEngine.Debug.Log("Deleted test lobby.");
    }
}
