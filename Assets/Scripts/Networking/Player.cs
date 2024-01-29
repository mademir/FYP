public class Player : COM.Client
{
    public Player(string name, string id, int udpPort)
    {
        Name = name;
        ID = id;
        UdpPort = udpPort;
    }

    public Player(COM.Client player)
    {
        Name = player.Name;
        ID = player.ID;
        UdpPort = player.UdpPort;
    }
}