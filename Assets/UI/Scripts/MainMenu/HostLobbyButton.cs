using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateLobbyButton : MonoBehaviour
{
    public string gameplaySceneName = "Game";
    public string hostDisplayName = "Host";
    public string region = "NA";
    public int maxPlayers = 8;
    public int port = 7777;

    public void OnClickCreate()
    {
        LaunchConfig.NextMode = LaunchConfig.Mode.Host;
        LaunchConfig.HostDisplayName = hostDisplayName;
        LaunchConfig.Region = region;
        LaunchConfig.MaxPlayers = maxPlayers;
        LaunchConfig.Port = port;

        Debug.Log("[CreateLobbyButton] Loading Scene: " + gameplaySceneName);
        SceneManager.LoadScene(gameplaySceneName);
    }
}
