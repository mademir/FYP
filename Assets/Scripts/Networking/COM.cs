namespace COM
{
    public class CreateLobbyInfo
    {
        public string LobbyName;
        public string PlayerName;

        public CreateLobbyInfo(string lobbyName, string playerName)
        {
            LobbyName = lobbyName;
            PlayerName = playerName;
        }
    }
}