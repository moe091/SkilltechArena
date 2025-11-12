using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;


public class SceneBootstrapper : MonoBehaviour
{

    public ServerLobbyConfig serverConfig;
    // Start is called before the first frame update
    void Start()
    {
        string exeDir = Path.GetDirectoryName(Application.dataPath);
        string cfgPath = Path.Combine(exeDir, "ServerLobbyConfig.json");

        if (CheckServerConfigExists(cfgPath))
        {
            Debug.Log($"Found Server config, parsing");
            ParseServerConfig(cfgPath);
        } else
        {
            Debug.Log("[Bootstrap] No ServerLobbyConfig.json found → loading MainMenu.");
            LoadMainMenu();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private bool CheckServerConfigExists(string cfgPath)
    {
        bool configExists = File.Exists(cfgPath);
        Debug.Log($"[Bootstrap] Looking for config at: {cfgPath}.");

        return configExists;
    }

    private void ParseServerConfig(string cfgPath)
    {
        string json = File.ReadAllText(cfgPath);
        serverConfig = JsonUtility.FromJson<ServerLobbyConfig>(json);
        Debug.Log($"[Bootstrap] Config loaded: {serverConfig.lobbyName}, port {serverConfig.port}, autoStart={serverConfig.autoStart}");

        if (serverConfig.autoStart == true)
        {
            Debug.Log("AutoStart True :: Loading ServerLoader Scene.");
            SceneManager.LoadScene("ServerLoader");
        }
    }


    private void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
