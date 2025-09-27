using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Assign in Inspector")]
    [SerializeField] private HUDManager hudManager;

    // Globally accessible HUDManager
    public static HUDManager HUDManager { get; private set; }

    private void Awake()
    {
        // Singleton guard
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   // remove if you don't need it across scenes

        HUDManager = hudManager;
        if (HUDManager == null)
            Debug.LogWarning("[GameManager] HUDManager is not assigned in the Inspector.");
    }
}
