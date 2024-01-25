public class Lobby : COM.Lobby
{
    /*public string Name { get; }
    public string ID { get; }
    public bool Full { get; }
    public Player PlayerA { get; }
    public Player PlayerB { get; }*/

    /*public Lobby(string name, string id, bool full)
    {
        Name = name;
        ID = id;
        Full = full;
    }*/

    public Lobby(COM.Lobby lobby)
    {
        Name = lobby.Name;
        ID = lobby.ID;
        Full = lobby.Full;
        PlayerA = lobby.PlayerA;
        PlayerB = lobby.PlayerB;
    }

    //Put Lobby info that should be available on client side but doesn't need to be on server here
}