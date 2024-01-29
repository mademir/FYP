using System.Net.Sockets;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Networking
{
    internal class Lobby : COM.Lobby
    {
        //public new Client PlayerA { get; set; }
        //public new Client PlayerB { get; set; }

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
            PlayerA = lobby.PlayerA; //(lobby.PlayerA != null ? new Client(lobby.PlayerA, null) : null);
            PlayerB = lobby.PlayerB; //(lobby.PlayerB != null ? new Client(lobby.PlayerB, null) : null);
            Full = lobby.Full;
            ID = lobby.ID;
        }

        internal void AddPlayerB(Client Client)
        {
            PlayerB = Client;
            Full = true;
        }

        internal void RemovePlayer(Client Client)
        {
            if (PlayerA?.ID == Client.ID)
            {
                PlayerA = PlayerB;
                PlayerB = null;
                if (PlayerA != null) PlayerA.LobbyLeader = true;
            }
            if (PlayerB?.ID == Client.ID)
            {
                PlayerB = null;
                if (PlayerA != null) PlayerA.LobbyLeader = true;
            }

            if ((PlayerA != null) || (PlayerB != null)) Full = false;
        }
    }
}