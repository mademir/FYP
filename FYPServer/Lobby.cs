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

        public Lobby(COM.Lobby lobby)
        {
            Name= lobby.Name;
            PlayerA = lobby.PlayerA;
            PlayerB = lobby.PlayerB;
            Full = lobby.Full;
            ID = lobby.ID;
        }

        internal void AddPlayerB(Client Client)
        {
            PlayerB = Client;
            Full = true;
        }
    }
}