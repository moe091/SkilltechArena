public static class LaunchConfig
{
    public enum Mode { None, Host, Client }

    // --- Connection / mode ---
    public static Mode NextMode = Mode.None;
    public static string Address = "127.0.0.1";
    public static int Port = 7777;

    // --- Lobby metadata (host only) ---
    public static string HostDisplayName = "Host";
    public static string Region = "NA";
    public static int MaxPlayers = 8;

    public static string Map = "Default";
    public static string GameMode = "Casual";
    public static string Version = "0.1.0";

    // --- Reset ---
    public static void Clear()
    {
        NextMode = Mode.None;
        Address = "127.0.0.1";
        Port = 7777;

        HostDisplayName = "Host";
        Region = "NA";
        MaxPlayers = 8;
        Map = "Default";
        GameMode = "Casual";
        Version = "0.1.0";
    }
}
