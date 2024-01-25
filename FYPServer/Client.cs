using System.Text.Json.Serialization;

namespace Networking
{
    public class Client : COM.Client
    {
        [JsonIgnore] public string IP;

        public Client(COM.Client client, string ip)
        {
            Name = client.Name;
            ID = client.ID;
            LobbyLeader = client.LobbyLeader;
            IP = ip;
        }
    }
}