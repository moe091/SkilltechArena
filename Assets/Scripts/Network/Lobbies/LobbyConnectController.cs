using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet;

public class ConnectionWatchdog : MonoBehaviour
{
    static ConnectionWatchdog _instance;

    string _menuScene;
    float _deadline;
    bool _armed;

    public static void Begin(float timeoutSeconds, string menuScene)
    {
        if (_instance == null)
        {
            var go = new GameObject("ConnectionWatchdog");
            _instance = go.AddComponent<ConnectionWatchdog>();
            DontDestroyOnLoad(go);
        }

        _instance._menuScene = menuScene;
        _instance._deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
        _instance._armed = true;
    }

    void Update()
    {
        if (!_armed) return;

        if (InstanceFinder.IsClientStarted || InstanceFinder.IsServerStarted)
        {
            // success: connected/hosted -> disarm
            _armed = false;
            return;
        }

        if (Time.realtimeSinceStartup >= _deadline)
        {
            _armed = false;

            // IMPORTANT: fully stop networking before scene change
            var nm = InstanceFinder.NetworkManager;
            if (nm != null)
            {
                if (nm.ClientManager.Started) nm.ClientManager.StopConnection();
                if (nm.ServerManager.Started) nm.ServerManager.StopConnection(true);
            }

            SceneManager.LoadScene(_menuScene);
        }
    }
}
