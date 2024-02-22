using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using UnityEngine;
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using COM;
using System.Linq;

public class Client : MonoBehaviour
{
    public string ServerIP = "127.0.0.1";
    TcpClient tcpClient;
    UdpClient udpClient;
    int tcpBufferSize = 5120;
    public int ServerTcpPort = 1025;
    public int ServerUdpPort = 1026;
    internal int LocalUdpPort;

    public bool hosting = false;
    public string plyrName = "";
    public bool send = false;
    public bool crea = false;
    public string msg = "";

    public GameController gameController;
    public VoiceChat VoiceChat;

    public string MyClientID;

    bool udpStreaming = false;
    bool sendUdp = false;
    IPEndPoint serverEP;
    ClientCOM.TransformInfo UdpTransformInfo;

    bool runOnBackground = true;

    List<NetworkNode> nodes = new List<NetworkNode>();
    public List<string> nodeTcpMessagePool = new List<string>();

    //DateTime lastPackageReceiveTime;
    //double ConnectionTimeout = 5.0;


    private void Start()
    {
        System.Random rnd = new System.Random();
        MyClientID = rnd.Next((int)Math.Pow(10, COM.Values.ClientIDLength - 1), (int)Math.Pow(10, COM.Values.ClientIDLength) - 1).ToString();

        // Get all network nodes
        nodes = FindObjectsOfType<NetworkNode>().ToList();

        // Setup UDP
        udpClient = new UdpClient(0); // Assign an available local port
        LocalUdpPort = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
        serverEP = new IPEndPoint(IPAddress.Parse(ServerIP), ServerUdpPort);

        ConnectToTCPServer();

        new Thread(() => ReceiveTCPMessage()).Start();
        new Thread(() => ReceiveUDPMessage()).Start();
        
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

    private void OnDestroy()
    {
        runOnBackground = false;
        tcpClient.Dispose();
        udpClient.Dispose();
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
            COM.Client player = new Player(plyrName, MyClientID, LocalUdpPort);
            var lobbyInfo = new COM.CreateLobbyInfo(msg, player);
            string jsonLobbyInfo = JsonConvert.SerializeObject(lobbyInfo);
            new Thread(() => SendTCPMessage("CREA" + jsonLobbyInfo)).Start();
        }

        if (nodeTcpMessagePool.Any()) new Thread(() => FlushNodeTcpMessagePool()).Start();
    }

    void FlushNodeTcpMessagePool()
    {
        foreach (string msg in nodeTcpMessagePool)
        {
            SendTCPMessage(msg);
        }
        nodeTcpMessagePool.Clear();
    }

    private void FixedUpdate()
    {
        if (udpStreaming)
        {
            UdpTransformInfo = new ClientCOM.TransformInfo(gameController.PlayerGO.transform);
            sendUdp = true;
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
        try
        {
            tcpClient = new TcpClient(ServerIP, ServerTcpPort);
            Debug.Log($"Connected to TCP server at {ServerIP}:{ServerTcpPort}");
            gameController.ShowConnectionLost = false;
        }
        catch (Exception e)
        {
            Debug.Log("TCP Connection Error: " + e.Message);
            gameController.ShowConnectionLost = true;
        }
    }

    public void SendTCPMessage(string message)
    {
        try
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] data = Encoding.ASCII.GetBytes(message + COM.Values.EOF);
            networkStream.Write(data, 0, data.Length);
            Debug.Log($"TCP Sent: {message}");
            gameController.ShowConnectionLost = false;
        }
        catch (Exception e)
        {
            Debug.Log("TCP Send Error: " + e.Message);
            gameController.ShowConnectionLost = true;
        }
    }

    public void ReceiveTCPMessage()
    {
        try
        {
            NetworkStream networkStream = tcpClient.GetStream();

            while (runOnBackground)
            {
                byte[] data = new byte[tcpBufferSize];
                int bytesRead = networkStream.Read(data, 0, data.Length);
                //lastPackageReceiveTime = DateTime.Now;
                gameController.ShowConnectionLost = false;
                string receivedMessage = Encoding.ASCII.GetString(data, 0, bytesRead);

                //Split merged messages
                var messages = receivedMessage.Split(COM.Values.EOF, StringSplitOptions.RemoveEmptyEntries);
                if (messages.Length > 1) { Debug.LogError("Multiple msgs received: " + receivedMessage); }
                foreach ( var message in messages ) HandleTCPMessage(message);
            }
        }
        catch (IOException e)
        {
            Debug.Log("TCP Receive IO Error: " + e.Message);
            gameController.ShowConnectionLost = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
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
            case "LOUP":
                ReceiveLobbyUpdate(data);
                break;
            case "FORW":
                ReceiveNodeUpdate(data);
                break;
            default:
                Console.WriteLine($"Unknown command: {command}");
                break;
        }
    }

    private void ReceiveNodeUpdate(string message)
    {
        if (message.Length < ClientCOM.Values.NodeIDLength) return;
        string nodeID = message.Substring(0, ClientCOM.Values.NodeIDLength);

        //Find node
        var ns = nodes.Where(n => n.ID == nodeID);
        if (!ns.Any()) return;
        var node = ns.First();

        string msg = message.Substring(ClientCOM.Values.NodeIDLength);
        if (msg.Length < NetworkNode.ActionCodes.ActionCodeLength) return;
        string actionCode = msg.Substring(0, NetworkNode.ActionCodes.ActionCodeLength);
        string data = msg.Substring(NetworkNode.ActionCodes.ActionCodeLength);

        switch (actionCode)
        {
            case NetworkNode.ActionCodes.SetMaterial:
                gameController.ExecuteOnMainThread.Add(() => node.SetMaterial(data, null, false));
                break;
            default:
                Console.WriteLine($"Unknown Network Node Action Code: {actionCode}");
                break;
        }
    }

    private void ReceiveLobbyList(string data)
    {
        var result = new List<Lobby>();

        List<COM.Lobby> lobbies = JsonConvert.DeserializeObject<List<COM.Lobby>>(data);

        //foreach (COM.Lobby lobby in lobbies) Debug.Log($"{lobby.Name}, {lobby.ID}, {lobby.Full}");

        foreach (COM.Lobby lobby in lobbies) result.Add(new Lobby(lobby));

        /*int ct = 0;

        foreach (COM.Lobby lobby in lobbies)
        {
            ct++;
            result.Add(new Lobby(lobby));
        }*/

        //gameController.UpdateLobbyList(result);
        gameController.ExecuteOnMainThread.Add(() => gameController.UpdateLobbyList(result));
    }

    private void ReceiveJoinLobbyConf(string data)
    {
        var lobby = new Lobby(JsonConvert.DeserializeObject<COM.Lobby>(data));
        gameController.ExecuteOnMainThread.Add(() => gameController.JoinLobby(lobby));
    }

    private void ReceiveLobbyUpdate(string data)
    {
        var lobby = new Lobby(JsonConvert.DeserializeObject<COM.Lobby>(data));
        if (gameController.CurrentAppState == GameController.AppState.Lobby)
        {
            gameController.lobbyController.MyLobby = lobby;

            if (lobby.CurrentGameState == GameState.InLobby)
                gameController.ExecuteOnMainThread.Add(() => gameController.lobbyController.UpdateLobby());

            if (lobby.CurrentGameState == GameState.InGame)
            {
                Debug.Log("Starting Game..");
                gameController.ExecuteOnMainThread.Add(() => gameController.PeerGO.SetActive(true));
                if (MyClientID == lobby.PlayerA.ID) gameController.ExecuteOnMainThread.Add(() => gameController.Teleport(gameController.PlayerGO, gameController.SpawnA.transform));
                if (MyClientID == lobby.PlayerB.ID) gameController.ExecuteOnMainThread.Add(() => {
                    gameController.Teleport(gameController.PlayerGO, gameController.SpawnB.transform);
                    gameController.PlayerGO.GetComponent<Renderer>().material = gameController.PlayerBMaterial;
                    gameController.PeerGO.GetComponent<Renderer>().material = gameController.PlayerAMaterial;
                });
                udpStreaming = true;
                new Thread(() => StartUdpStream()).Start();
                gameController.ExecuteOnMainThread.Add(() => gameController.SwitchToGame());
                if (MyClientID == lobby.PlayerA.ID) gameController.ExecuteOnMainThread.Add(() => gameController.PressurePlatePuzzle.SetupPuzzle()); // Only let Player A setup the puzzle
            }

        }
    }

    private void StartUdpStream()
    {
        var tempTransformInfo = new ClientCOM.TransformInfo();

        while (udpStreaming)
        {
            if (sendUdp)
            {
                if (!ClientCOM.TransformInfo.Compare(tempTransformInfo, UdpTransformInfo))
                {
                    string msg = ClientCOM.Values.TransformTag + UdpTransformInfo.Serialise();
                    SendUDPMessage(msg);

                    tempTransformInfo = UdpTransformInfo;
                }

                sendUdp = false;
            }
        }
    }

    public void SendUDPMessage(string message)
    {
        try
        {
            message = MyClientID + message;
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
        Debug.Log($"Starting UDP Listener on port: {LocalUdpPort}");
        while (runOnBackground)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEP);
                string receivedMessage = Encoding.ASCII.GetString(data);
                Debug.Log($"UDP Received: {receivedMessage}");
                ParseUdpMessage(receivedMessage);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private void ParseUdpMessage(string msg)
    {
        if (msg.Length < ClientCOM.Values.TagLength)
        {
            Console.WriteLine($"Unknown command: {msg}");
            return;
        }
        string command = msg.Substring(0, ClientCOM.Values.TagLength);
        string data = msg.Substring(ClientCOM.Values.TagLength);

        switch (command)
        {
            case ClientCOM.Values.TransformTag:
                SetPeerTransform(data);
                break;
            case ClientCOM.Values.VoiceTag:
                VoiceChat.VoiceData.Push(data);
                break;
            default:
                Console.WriteLine($"Unknown command: {command}");
                break;
        };
    }

    private void SetPeerTransform(string data)
    {
        var trans = new ClientCOM.TransformInfo(data);
        gameController.ExecuteOnMainThread.Add(() => gameController.PeerGO.transform.position = trans.Position);
        gameController.ExecuteOnMainThread.Add(() => gameController.PeerGO.transform.rotation = trans.Rotation);
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

    public void ReqCreateLobby()
    {
        var player = gameController.MyPlayer;
        if (player != null)
        {
            var lobbyInfo = new COM.CreateLobbyInfo($"Room of {player.Name}", player);
            string jsonLobbyInfo = JsonConvert.SerializeObject(lobbyInfo);
            new Thread(() => SendTCPMessage("CREA" + jsonLobbyInfo)).Start();
        }
    }

    internal void ReqJoinLobby(string lobbyID, Player player)
    {
        string jsonJoinLobbyInfo = JsonConvert.SerializeObject(new COM.JoinLeaveLobbyInfo(lobbyID, player));

        new Thread(() => SendTCPMessage("JOIN" + jsonJoinLobbyInfo)).Start();
    }

    internal void ReqLeaveLobby()
    {
        string jsonJoinLobbyInfo = JsonConvert.SerializeObject(new COM.JoinLeaveLobbyInfo(gameController.lobbyController.MyLobby.ID, gameController.MyPlayer));

        new Thread(() => SendTCPMessage("LEAV" + jsonJoinLobbyInfo)).Start();
    }

    public void ReqStartGame()
    {
        string jsonStartGameInfo = JsonConvert.SerializeObject(new COM.StartGameInfo(gameController.lobbyController.MyLobby.ID, MyClientID));

        new Thread(() => SendTCPMessage("STRT" + jsonStartGameInfo)).Start();
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
