using System.Text.Json.Serialization;

namespace Networking
{
    public class Client
    {
        public string Name;
        [JsonIgnore] public string IP;
        public bool LobbyLeader = false;

        public Client(string name, string clientIP, bool lobbyLeader)
        {
            Name = name;
            IP = clientIP;
            LobbyLeader = lobbyLeader;
        }

    }
}