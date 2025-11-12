[System.Serializable]
public class ServerLobbyConfig
{
    public string lobbyName;
    public string region;
    public int maxPlayers;
    public int matchLength;
    public int transitionLength;
    public string map;
    public string publicIP;
    public int port;
    public bool autoStart;
    public string databaseUrl;
}