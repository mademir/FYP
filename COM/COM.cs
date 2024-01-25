namespace COM
{
    public class CreateLobbyInfo
    {
        public string LobbyName { get; set; }
        public Client Player { get; set; }


        public CreateLobbyInfo(string lobbyName, Client player)
        {
            LobbyName = lobbyName;
            Player = player;
        }
    }

    /*public class CreateLobbyInfo
    {
        public string LobbyName { get; set; }
        public string PlayerName { get; set; }


        public CreateLobbyInfo(string lobbyName, string playerName)
        {
            LobbyName = lobbyName;
            PlayerName = playerName;
        }
    }*/

    public class JoinLobbyInfo
    {
        public string LobbyID { get; set; }
        public Client Player { get; set; }

        public JoinLobbyInfo(string lobbyID, Client player)
        {
            LobbyID = lobbyID;
            Player = player;
        }
    }

    /*public class JoinLobbyConf
    {
        private string LobbyID { get; set; }

        public JoinLobbyConf(string lobbyID)
        {
            LobbyID = lobbyID;
        }
    }*/

    // Use lobby instead  \/

    public class Client
    {
        public string Name;
        public string ID;
        public bool LobbyLeader = false;
    }

    public class Lobby
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public Client PlayerA { get; set; }
        public Client PlayerB { get; set; }
        public bool Full { get; set; }
    }
}