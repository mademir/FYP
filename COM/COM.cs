﻿namespace COM
{

    public static class Values
    {
        public static string EOF = "<EOF>";
        public static int ClientIDLength = 7;
    }

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

    public class JoinLeaveLobbyInfo
    {
        public string LobbyID { get; set; }
        public Client Player { get; set; }

        public JoinLeaveLobbyInfo(string lobbyID, Client player)
        {
            LobbyID = lobbyID;
            Player = player;
        }
    }

    public class StartEndGameInfo
    {
        public string LobbyID { get; set; }
        public string PlayerID { get; set; }

        public StartEndGameInfo(string lobbyID, string playerID)
        {
            LobbyID = lobbyID;
            PlayerID = playerID;
        }
    }

    public class LobbyNameUpdateInfo
    {
        public string LobbyID { get; set; }
        public string PlayerID { get; set; }
        public string NewName { get; set; }

        public LobbyNameUpdateInfo(string lobbyID, string playerID, string newName)
        {
            LobbyID = lobbyID;
            PlayerID = playerID;
            NewName = newName;
        }
    }

    public class Client
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public int UdpPort { get; set; }
        public bool LobbyLeader { get; set; } = false;
    }

    public class Lobby
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public Client? PlayerA { get; set; }
        public Client? PlayerB { get; set; }
        public bool Full { get; set; }
        public GameState CurrentGameState { get; set; } = GameState.InLobby;
    }

    public enum GameState
    {
        InLobby,
        InGame,
        EndGame
    }
}