public class Lobby : COM.Lobby
{
    /*public string Name { get; }
    public string ID { get; }
    public bool Full { get; }*/

    /*public Lobby(string name, string id, bool full)
    {
        Name = name;
        ID = id;
        Full = full;
    }*/

    //public new Player PlayerA { get; set; }
    //public new Player PlayerB { get; set; }

    public Lobby(COM.Lobby lobby)
    {
        Name = lobby.Name;
        ID = lobby.ID;
        Full = lobby.Full;
        PlayerA = lobby.PlayerA; //(lobby.PlayerA != null ? new Player(lobby.PlayerA) : null);
        PlayerB = lobby.PlayerB; //(lobby.PlayerB != null ? new Player(lobby.PlayerB) : null);
        CurrentGameState = lobby.CurrentGameState;
    }

    //Put Lobby info that should be available on client side but doesn't need to be on server here
}