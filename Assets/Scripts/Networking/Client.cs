using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using UnityEngine;
using System.Threading;
using UnityEditor.Experimental.GraphView;
using Newtonsoft.Json;

public class Client : MonoBehaviour
{
    public string IP = "127.0.0.1";
    TcpClient tcpClient;
    UdpClient udpClient;
    int tcpBufferSize = 512; //Put in com?
    public int tcpPort = 1025;
    public int udpPort = 1026;

    public bool hosting = false;
    public string plyrName = "";
    public bool send = false;
    public bool crea = false;
    public string msg = "";

    public GameController gameController;

    public string MyClientID;

    //DateTime lastPackageReceiveTime;
    //double ConnectionTimeout = 5.0;

    /* TODO:
     * Lobby communication. Receive Pair info (IP) and if hosting or not.
     * After receiving this, load main scene and connect udp to pair GO location etc.
     */


    private void Start()
    {
        System.Random rnd = new System.Random();
        MyClientID = rnd.Next(10000, 99999).ToString();

        ConnectToTCPServer();

        new Thread(() => ReceiveTCPMessage()).Start();
        
        /*
        // Connect to the TCP server
        ConnectToTCPServer(IP, tcpPort);

        // Send and receive TCP messages
        SendTCPMessage("Hello from TCP client!");
        ReceiveTCPMessage();

        // Send and receive UDP messages
        SendUDPMessage("Hello from UDP client!", IP, udpPort);
        ReceiveUDPMessage();*/
    }

    private void Update()
    {
        //if ((DateTime.Now - lastPackageReceiveTime).TotalSeconds > ConnectionTimeout) { }


        if (send)
        {
            send = false;
            new Thread(() => SendTCPMessage(msg)).Start();
        }
        
        if (crea)
        {
            crea = false;
            COM.Client player = new Player(plyrName, MyClientID);
            var lobbyInfo = new COM.CreateLobbyInfo(msg, player);
            string jsonLobbyInfo = JsonConvert.SerializeObject(lobbyInfo);
            new Thread(() => SendTCPMessage("CREA" + jsonLobbyInfo)).Start();
        }
    }

    static void StartUDPServer(int port)
    {
        UdpClient udpListener = null;

        try
        {
            udpListener = new UdpClient(port);
            Console.WriteLine($"UDP Server listening on port {port}...");

            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            string message = "";
            while (message != "exit")
            {
                byte[] data = udpListener.Receive(ref remoteEP);
                message = Encoding.ASCII.GetString(data);
                Console.WriteLine($"UDP Received: {message}");

                // Send a response back to the client.
                byte[] response = Encoding.ASCII.GetBytes($"You sent: {message}");
                udpListener.Send(response, response.Length, remoteEP);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("UDP Error: " + e.Message);
        }
        finally
        {
            udpListener?.Close();
        }
    }

    public void ConnectToTCPServer()
    {
        string ipAddress = IP;
        int port = tcpPort;
        try
        {
            tcpClient = new TcpClient(ipAddress, port);
            Debug.Log($"Connected to TCP server at {ipAddress}:{port}");
            gameController.ShowConnectionLost(false);
        }
        catch (Exception e)
        {
            Debug.Log("TCP Connection Error: " + e.Message);
            gameController.ShowConnectionLost(true);
        }
    }

    public void SendTCPMessage(string message)
    {
        try
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] data = Encoding.ASCII.GetBytes(message);
            networkStream.Write(data, 0, data.Length);
            Debug.Log($"TCP Sent: {message}");
            gameController.ShowConnectionLost(false);
        }
        catch (Exception e)
        {
            Debug.Log("TCP Send Error: " + e.Message);
            gameController.ShowConnectionLost(true);
        }
    }

    public void ReceiveTCPMessage()
    {
        try
        {
            NetworkStream networkStream = tcpClient.GetStream();

            while (true)
            {
                byte[] data = new byte[tcpBufferSize];
                int bytesRead = networkStream.Read(data, 0, data.Length);
                //lastPackageReceiveTime = DateTime.Now;
                gameController.ShowConnectionLost(false);
                string receivedMessage = Encoding.ASCII.GetString(data, 0, bytesRead);
                HandleTCPMessage(receivedMessage);
            }
        }
        catch (Exception e)
        {
            Debug.Log("TCP Receive Error: " + e.Message);
            gameController.ShowConnectionLost(true);
        }
    }

    private void HandleTCPMessage(string msg)
    {
        Debug.Log($"TCP Received: {msg}");

        if (msg.Length < 4)
        {
            Console.WriteLine($"Unknown command: {msg}");
            return;
        }
        string command = msg.Substring(0, 4);
        string data = msg.Substring(4);

        switch (command)
        {
            case "LIST":
                ReceiveLobbyList(data);
                break;
            case "JOIN":
                ReceiveJoinLobbyConf(data);
                break;
            default:
                Console.WriteLine($"Unknown command: {command}");
                break;
        }
    }

    private void ReceiveLobbyList(string data)
    {
        var result = new List<Lobby>();

        List<COM.Lobby> lobbies = JsonConvert.DeserializeObject<List<COM.Lobby>>(data);

        //foreach (COM.Lobby lobby in lobbies) Debug.Log($"{lobby.Name}, {lobby.ID}, {lobby.Full}");

        foreach (COM.Lobby lobby in lobbies) result.Add(new Lobby(lobby));

        Debug.Log($"Received {result.Count} lobbies.");

        gameController.UpdateLobbyList(result);
    }

    private void ReceiveJoinLobbyConf(string data)
    {
        var lobby = JsonConvert.DeserializeObject<COM.Lobby>(data) as Lobby;
        gameController.JoinLobby(lobby);
    }

    public void SendUDPMessage(string message, string ipAddress, int port)
    {
        try
        {
            udpClient = new UdpClient();
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            byte[] data = Encoding.ASCII.GetBytes(message);
            udpClient.Send(data, data.Length, serverEP);
            Debug.Log($"UDP Sent: {message}");
        }
        catch (Exception e)
        {
            Debug.Log("UDP Send Error: " + e.Message);
        }
    }

    public void ReceiveUDPMessage()
    {
        try
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpClient.Receive(ref remoteEP);
            string receivedMessage = Encoding.ASCII.GetString(data);
            Debug.Log($"UDP Received: {receivedMessage}");
        }
        catch (Exception e)
        {
            Debug.Log("UDP Receive Error: " + e.Message);
        }
    }


    public void CheckConnection()
    {
        if (tcpClient != null)
        {
            SendTCPMessage("PING");
            if (!tcpClient.Connected) ConnectToTCPServer();
        }
    }
    internal void Disconnect()
    {
        tcpClient?.Close();
    }

    public void ReqListLobbies()
    {
        new Thread(() => SendTCPMessage("LIST")).Start();
    }

    internal void ReqJoinLobby(string lobbyID, Player player)
    {
        string jsonJoinLobbyInfo = JsonConvert.SerializeObject(new COM.JoinLobbyInfo(lobbyID, player));

        new Thread(() => SendTCPMessage("JOIN" + jsonJoinLobbyInfo)).Start();
    }

    internal void ReqLeaveLobby()
    {
        throw new NotImplementedException();
    }







    /*public string IP = "127.0.0.1";
    TcpClient tcpClient;
    UdpClient udpClient;
    public int tcpBufferSize = 256;
    public int tcpPort = 1025;
    public int udpPort = 1026;
    
    public bool hosting = false;

    public string msg;
    public UDPChannel channel;
    public bool send = false;

    IPEndPoint serverEP;
    IPEndPoint remoteEP;

    private void Start()
    {
        udpClient = new UdpClient(udpPort);
        serverEP = new IPEndPoint(IPAddress.Parse(IP), udpPort);
        remoteEP = new IPEndPoint(IPAddress.Any, 0);


        // Connect to the TCP server
        //ConnectToTCPServer(IP, tcpPort);

        // Send and receive TCP messages
        //SendTCPMessage("Hello from TCP client!");
        //ReceiveTCPMessage();

        // Send and receive UDP messages
        //SendUDPMessage("Hello from UDP client!", IP, udpPort);
        //ReceiveUDPMessage();
    }

    private void Update()
    {
        if (send)
        {
            send = false;
            new Thread(() => SendUDPMessage(msg, IP, udpPort, channel)).Start();
            new Thread(() => ReceiveUDPMessage()).Start();
        }
    }

    public void ConnectToTCPServer(string ipAddress, int port)
    {
        try
        {
            tcpClient = new TcpClient(ipAddress, port);
            Debug.Log($"Connected to TCP server at {ipAddress}:{port}");
        }
        catch (Exception e)
        {
            Debug.Log("TCP Connection Error: " + e.Message);
        }
    }

    public void SendTCPMessage(string message)
    {
        try
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] data = Encoding.ASCII.GetBytes(message);
            networkStream.Write(data, 0, data.Length);
            Debug.Log($"TCP Sent: {message}");
        }
        catch (Exception e)
        {
            Debug.Log("TCP Send Error: " + e.Message);
        }
    }

    public void ReceiveTCPMessage()
    {
        try
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] data = new byte[tcpBufferSize];
            int bytesRead = networkStream.Read(data, 0, data.Length);
            string receivedMessage = Encoding.ASCII.GetString(data, 0, bytesRead);
            Debug.Log($"TCP Received: {receivedMessage}");
        }
        catch (Exception e)
        {
            Debug.Log("TCP Receive Error: " + e.Message);
        }
    }

    public void SendUDPMessage(string message, string ipAddress, int port, UDPChannel channel)
    {
        try
        {
            //IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            byte[] data = Encoding.ASCII.GetBytes(message+channel.ToString());
            udpClient.Send(data, data.Length, serverEP);
            Debug.Log($"UDP Sent: {message}");
        }
        catch (Exception e)
        {
            Debug.Log("UDP Send Error: " + e.Message);
        }
    }

    public void ReceiveUDPMessage()
    {
        try
        {
            //IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpClient.Receive(ref remoteEP);
            string receivedMessage = Encoding.ASCII.GetString(data);
            Debug.Log($"UDP Received: {receivedMessage}");
        }
        catch (Exception e)
        {
            Debug.Log("UDP Receive Error: " + e.Message);
        }
    }
    
    public enum UDPChannel
    {
        Transform,
        Message
    }*/
}
