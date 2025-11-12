using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerAutostarter : MonoBehaviour
{
    [SerializeField] private FirebaseLobbyDirectory lobbyDirectory;
    [SerializeField] private string gameplaySceneName = "SampleScene";

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

        Debug.Log("[CreateLobbyButton] Loading Scene: " + gameplaySceneName);
        SceneManager.LoadScene(gameplaySceneName);
    }

}
