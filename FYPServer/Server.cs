using COM;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Networking {
    class Server
    {
        static object lobbiesLock = new object();
        static List<Lobby> lobbies = new List<Lobby>();
        //static int TCPTicks = 10;

        //TODO: Implement ticks for the server and tcp handler loops

        static void Main()
        {
            IPAddress ServerIP = Dns.GetHostAddresses(Dns.GetHostName())[3];
            Console.WriteLine($"Server IP: {ServerIP}");

            // Open tcp server.
            // Tcp handler should handle listing, creating and joining lobbies.


            TcpListener tcpListener = null;

            try
            {
                tcpListener = new TcpListener(ServerIP, tcpPort);
                tcpListener.Start();

                Console.WriteLine($"Server listening on port {tcpPort}...");

                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    Console.WriteLine($"ACCEPTED: {tcpClient}");

                    // Create a new thread to handle the TCP client
                    Thread clientThread = new Thread(() => HandleTCPClient(tcpClient));
                    clientThread.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("TCP Server Error: " + e.Message);
            }
            finally
            {
                tcpListener?.Stop();
            }
        }

        static void HandleTCPClient(TcpClient tcpClient)
        {
            try
            {
                string message = "";
                NetworkStream networkStream = tcpClient.GetStream();
                /*Stopwatch sw = Stopwatch.StartNew();
                int ct = 0; */
                while (!message.ToLower().StartsWith("exit"))
                {
                    //sw.Restart();
                    byte[] data = new byte[tcpBufferSize];

                    if(!networkStream.DataAvailable) continue;
                    int bytesRead = networkStream.Read(data, 0, data.Length);
                    message = Encoding.ASCII.GetString(data, 0, bytesRead);
                    Console.WriteLine($"{tcpClient}: {message}");

                    ParseTCPRequest(message, tcpClient);

                    // Send a response back to the client.
                    //byte[] response = Encoding.ASCII.GetBytes($"You sent: {message}");
                    //networkStream.Write(response, 0, response.Length);

                    /*sw.Stop();
                    int timeToWait = (int) (((long)1000 / TCPTicks) - sw.ElapsedMilliseconds);
                    Console.WriteLine($"T: {timeToWait}\tct: {ct++}");
                    Thread.Sleep(timeToWait);*/
                }

                // Close the connection.
                tcpClient.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("TCP Error: " + e.Message);
            }
        }
        static void SendTCPMessage(string message, TcpClient tcpClient)
        {
            byte[] responseData = Encoding.ASCII.GetBytes(message);
            tcpClient.GetStream().Write(responseData, 0, responseData.Length);
        }

        private static void ParseTCPRequest(string request, TcpClient tcpClient)
        {
            if (request.Length < 4)
            {
                Console.WriteLine($"Unknown command: {request}");
                return;
            }
            string command = request.Substring(0, 4);
            string data = request.Substring(4);

            switch (command)
            {
                case "LIST":
                    SendLobbyList(tcpClient);
                    break;
                case "CREA":
                    CreateLobby(data, tcpClient);
                    break;
                case "JOIN":
                    JoinLobby(data, tcpClient);
                    break;
                case "PING":
                    SendTCPMessage("PONG", tcpClient);
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }

        static void SendLobbyList(TcpClient tcpClient)
        {
            lock (lobbiesLock)
            {
                //string response = "LIST" + JsonSerializer.Serialize(lobbies.Select(lobby => lobby.Name).ToList());
                var jsonLobbies = lobbies.Select(x => x as COM.Lobby).ToList();
                string response = "LIST" + JsonSerializer.Serialize(jsonLobbies);

                SendTCPMessage(response, tcpClient);
            }
        }

        static void CreateLobby(string data, TcpClient tcpClient)
        {
            var lobbyInfo = JsonSerializer.Deserialize<COM.CreateLobbyInfo>(data);
            if (lobbyInfo == null) return;
            if (lobbyInfo.LobbyName == "") return;

            lock (lobbiesLock)
            {
                if (lobbies.Where(lobby => lobby.Name == lobbyInfo.LobbyName).Count() == 0)
                {
                    string clientIP = tcpClient.Client.RemoteEndPoint != null ? (tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.ToString() : "";
                    Console.WriteLine($"Client IP: {clientIP}");
                    //lobbies.Add(new Lobby(lobbyInfo.LobbyName, new Client(lobbyInfo.Player.Name, clientIP, true)));
                    //var player = lobbyInfo.Player as Client;
                    //player.IP = clientIP;
                    //player.LobbyLeader = true;

                    //var player = new Client(lobbyInfo.Player.Name, clientIP, true);
                    //player.ID = lobbyInfo.Player.ID;

                    /*var player = lobbyInfo.Player as Client;
                    player.IP = clientIP;
                    var lobby = new Lobby(lobbyInfo.LobbyName, player);*/

                    var player = new Client(lobbyInfo.Player, clientIP);
                    var lobby = new Lobby(lobbyInfo.LobbyName, player);

                    lobbies.Add(lobby);
                    Console.WriteLine($"Lobby '{lobbyInfo.LobbyName}' created by '{lobbyInfo.Player.Name}'.");

                    // Send confirmation

                    SendJoinLobbyConfirmation(lobby, tcpClient);
                }
                else
                {
                    Console.WriteLine($"Lobby '{lobbyInfo.LobbyName}' already exists.");
                }
            }
            /*
            if (lobbyName == "") return;

            lock (lobbiesLock)
            {
                if (lobbies.Where(lobby => lobby.Name ==  lobbyName).Count() == 0)
                {
                    string clientIP = tcpClient.Client.RemoteEndPoint != null ? (tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.ToString() : "";
                    Console.WriteLine($"Client IP: {clientIP}");
                    lobbies.Add(new Lobby(lobbyName, new Client(lobbyName, clientIP, true)));//GET ACTUAL CLIENT NAME
                    Console.WriteLine($"Lobby '{lobbyName}' created by a player.");
                }
                else
                {
                    Console.WriteLine($"Lobby '{lobbyName}' already exists.");
                }
            }*/
        }

        static void JoinLobby(string data, TcpClient tcpClient)
        {
            var lobbyInfo = JsonSerializer.Deserialize<COM.JoinLobbyInfo>(data);
            if (lobbyInfo == null) return;

            var lobbyID = lobbyInfo.LobbyID;
            if (lobbyID == "") return;

            lock (lobbiesLock)
            {
                List<Lobby> lobbies = Server.lobbies.Where(lobby => lobby.ID == lobbyID).ToList();

                if (lobbies.Any())
                {
                    var lobby = lobbies[0];
                    if (lobby.Full)
                    {
                        Console.WriteLine($"Lobby '{lobby.Name}' is full.");
                        return;
                    }

                    string clientIP = tcpClient.Client.RemoteEndPoint != null ? (tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.ToString() : "";
                    var player = lobbyInfo.Player as Client;
                    player.IP = clientIP;
                    lobby.AddPlayerB(player);
                    Console.WriteLine($"{lobbyInfo.Player.Name} joined lobby '{lobby.Name}'.");

                    // Send confirmation

                    SendJoinLobbyConfirmation(lobby, tcpClient);
                }
                else
                {
                    Console.WriteLine($"Lobby '{lobbyID}' does not exist.");
                }
            }

            /*lock (lobbiesLock)
            {
                List<Lobby> lobbies = Server.lobbies.Where(lobby => lobby.ID == lobbyID).ToList();

                if (lobbies.Count > 0)
                {
                    var lobby = lobbies[0];
                    if (lobby.Full)
                    {
                        Console.WriteLine($"Lobby '{lobby.Name}' is full.");
                        return;
                    }

                    string clientIP = tcpClient.Client.RemoteEndPoint != null ? (tcpClient.Client.RemoteEndPoint as IPEndPoint).Address.ToString() : "";
                    lobby.AddPlayerB(new Client(lobby.Name + "B", clientIP, false));//GET ACTUAL CLIENT NAME
                    Console.WriteLine($"Player joined lobby '{lobby.Name}'.");
                }
                else
                {
                    Console.WriteLine($"Lobby '{lobbyID}' does not exist.");
                }
            }*/
        }

        private static void SendJoinLobbyConfirmation(Lobby lobby, TcpClient tcpClient)
        {
            string jsonJoinLobbyConf = JsonSerializer.Serialize(lobby as COM.Lobby);
            SendTCPMessage("JOIN" + jsonJoinLobbyConf, tcpClient);
        }
















        /*public static int MaxClients { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public static void Start(int maxClients, int port)
        {
            MaxClients = maxClients;
            Port = port;

            Console.WriteLine("Starting the server.");

            for (int i = 1; i <= MaxClients; i++)
            {
                clients.Add(i, new Client(i));
            }

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine($"Server started on port {Port}.");
        }*/

        static String IP = "192.168.1.46";//"192.168.1.172";//"127.0.0.1";
        static int tcpBufferSize = 512;
        static int tcpPort = 1025;
        static int udpPort = 1026;

        /*static void Main()
        {
            var uc = new udpCom();
            uc.initClient(IP, udpPort);
            while (true) {
                uc.SendUDPMessage(DateTime.Now.ToString(), IP, udpPort, udpCom.UDPChannel.Message);
                //uc.ReceiveUDPMessage();
            }
            
            // Start TCP server thread
            //Thread tcpThread = new Thread(() => StartTCPServer(tcpPort));
            //tcpThread.Start();

            // Start UDP server thread
            //Thread udpThread = new Thread(() => StartUDPServer(udpPort));
            //udpThread.Start();

            Console.WriteLine("Threaded server running...");

            // Keep the main thread running
            //Console.ReadLine();
        }*/

        class udpCom
        {
            public enum UDPChannel
            {
                Transform,
                Message
            }
            TcpClient tcpClient;
            UdpClient udpClient;
            IPEndPoint serverEP;
            IPEndPoint remoteEP;

            public void initClient(string ipAddress, int port) {
                udpClient = new UdpClient(port);
                serverEP = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                remoteEP = new IPEndPoint(IPAddress.Any, 0);
            }

            public void SendUDPMessage(string message, string ipAddress, int port, UDPChannel channel)
            {
                try
                {
                    //IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    udpClient.Send(data, data.Length, serverEP);
                    Console.WriteLine($"UDP Sent: {message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine("UDP Send Error: " + e.Message);
                }
            }

            public void ReceiveUDPMessage()
            {
                try
                {
                    //IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpClient.Receive(ref remoteEP);
                    string receivedMessage = Encoding.ASCII.GetString(data);
                    Console.WriteLine($"UDP Received: {receivedMessage}");
                }
                catch (Exception e)
                {
                    Console.WriteLine("UDP Receive Error: " + e.Message);
                }
            }
        }










        static void StartTCPServer(int port)
        {
            TcpListener tcpListener = null;
            TcpClient tcpClient = null;

            try
            {
                tcpListener = new TcpListener(IPAddress.Parse(IP), port);
                tcpListener.Start();

                Console.WriteLine($"TCP Server listening on port {port}...");

                while (true)
                {
                    tcpClient = tcpListener.AcceptTcpClient();

                    // Create a new thread to handle the TCP client
                    Thread clientThread = new Thread(() => HandleTCPClient(tcpClient));
                    clientThread.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("TCP Error: " + e.Message);
            }
            finally
            {
                tcpListener?.Stop();
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
    }
}