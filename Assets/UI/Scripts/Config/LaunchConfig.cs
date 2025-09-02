public static class LaunchConfig
{
    public enum Mode { None, Host, Client }

    public static Mode NextMode = Mode.None;
    public static string Address = "127.0.0.1";
    public static int Port = 7777;

    // Optional display/meta used by Firebase publisher when hosting:
    public static string HostDisplayName = "Host";
    public static string Region = "NA";
    public static int MaxPlayers = 8;

    public static void Clear()
    {
        NextMode = Mode.None;
        Address = "127.0.0.1";
        Port = 7777;
    }
}
