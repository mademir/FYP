using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace Networking
{
    internal class Lobby
    {
        public string Name { get; set; }
        public string ID { get; set; }
        [JsonIgnore] public Client PlayerA { get; set; }
        [JsonIgnore] public Client PlayerB { get; set; }
        public bool Full { get; set; }

        public Lobby(string lobbyName, Client playerA)
        {
            Name = lobbyName;
            PlayerA = playerA;
            Full = false;
            Random rnd = new Random();
            ID = rnd.Next(10000, 99999).ToString();
        }

        internal void AddPlayerB(Client Client)
        {
            PlayerB = Client;
            Full = true;
        }
    }
}