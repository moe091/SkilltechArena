using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerAutostarter : MonoBehaviour
{
    [SerializeField] private FirebaseLobbyDirectory lobbyDirectory;
    [SerializeField] private string gameplaySceneName = "SampleScene"; //NOTE:: after I make actual maps, this will be read from _cfg.map, and each map will have it's own scene with matching name

    private SceneBootstrapper _bootstrap;
    private ServerLobbyConfig _cfg;

    

    void Start()
    {
        _bootstrap = FindObjectOfType<SceneBootstrapper>(includeInactive: true);
        if (_bootstrap == null)
        {
            Debug.LogError("[ServerAutoStarter] SceneBootstrapper not found. Loading MainMenu.");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        _cfg = _bootstrap.serverConfig;


        CreateLobby();
    }


    private void CreateLobby()
    {
        LaunchConfig.NextMode = LaunchConfig.Mode.Server;
        LaunchConfig.HostDisplayName = _cfg.lobbyName;
        LaunchConfig.Region = _cfg.region;
        LaunchConfig.MaxPlayers = _cfg.maxPlayers;
        LaunchConfig.Port = _cfg.port;
        LaunchConfig.Address = _cfg.publicIP;

        Debug.Log("[CreateLobbyButton] Creating lobby: " + LaunchConfig.Address);
        Debug.Log("[CreateLobbyButton] Loading Scene: " + gameplaySceneName);
        SceneManager.LoadScene(gameplaySceneName);
    }

}
