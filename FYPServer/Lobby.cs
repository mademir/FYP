using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace Networking
{
    internal class Lobby : COM.Lobby
    {
        public Lobby(string lobbyName, Client playerA)
        {
            Name = lobbyName;
            PlayerA = playerA;
            PlayerA.LobbyLeader = true;
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