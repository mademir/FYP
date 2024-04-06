using COM;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Networking {
    class Server
    {

        //static String IP = "192.168.1.46";//"192.168.1.172";//"127.0.0.1";
        static int tcpBufferSize = 5120;
        static int tcpPort = 1025;
        static int udpPort = 1026;

        static object lobbiesLock = new object();
        static List<Lobby> lobbies = new List<Lobby>();

        static Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        static Dictionary<string, string> recipients = new Dictionary<string, string>();
        static Dictionary<string, IPEndPoint> udpRecipients = new Dictionary<string, IPEndPoint>();

        //static int TCPTicks = 10;

        //TODO: Implement ticks for the server and tcp handler loops

        static void Main()
        {
            IPAddress ServerIP = Dns.GetHostAddresses(Dns.GetHostName())[3];
            Console.WriteLine($"Server IP: {ServerIP}");

            // Start udp server on a new thread.

            Thread UdpServer = new Thread(() => StartUdpServer());
            UdpServer.Start();

            // Start tcp server on the main thread.

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

        static void StartUdpServer()
        {
            UdpClient udpListener = new UdpClient(udpPort);
            Console.WriteLine($"UDP Server listening on port {udpPort}...");

            while (true)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpListener.Receive(ref remoteEP);
                    string message = Encoding.ASCII.GetString(data);
                    Console.WriteLine($"UDP Received: {message}");

                    // Create a new thread to handle the UDP message
                    //Thread clientThread = new Thread(() => HandleUdpMessage(message, remoteEP));

                    //Get the sender client ID from data and determine the recipient from that.
                    if (message.Length < COM.Values.ClientIDLength) continue;
                    string senderClientID = message.Substring(0, COM.Values.ClientIDLength);
                    var recipient = udpRecipients[senderClientID];
                    Console.WriteLine($"Sending to recipient on {recipient.Address}:{recipient.Port}");

                    int offset = Encoding.ASCII.GetByteCount(senderClientID);
                    byte[] response = new byte[data.Length - offset];
                    Buffer.BlockCopy(data, offset, response, 0, response.Length);

                    //byte[] response = Encoding.ASCII.GetBytes(message.Substring(COM.Values.ClientIDLength));

                    // Send a message to its recipient.
                    udpListener.Send(response, response.Length, recipient);
                }
                catch (Exception e)
                {
                    Console.WriteLine("UDP Error: " + e.Message);
                }
            }
            
            //udpListener?.Close();
        }

        /*private static void HandleUdpMessage(string message, IPEndPoint remoteEP)
        {
            throw new NotImplementedException();
        }*/

        static void HandleTCPClient(TcpClient tcpClient)
        {
            string message = "";
            NetworkStream networkStream = tcpClient.GetStream();
            /*Stopwatch sw = Stopwatch.StartNew();
            int ct = 0; */
            int lastBytesRead = 1;

            while (!message.ToLower().StartsWith("exit"))
            {
                try
                {
                    //sw.Restart();
                    byte[] data = new byte[tcpBufferSize];

                    //if(!networkStream.DataAvailable) continue;
                    int bytesRead = networkStream.Read(data, 0, data.Length);
                    if (bytesRead == 0 && lastBytesRead == 0) break;    // If last 2 reads have been empty, close the connection
                    lastBytesRead = bytesRead;

                    message = Encoding.ASCII.GetString(data, 0, bytesRead);

                    // Split merged messages
                    var messages = message.Split(COM.Values.EOF, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string msg in messages)
                    {
                        Console.WriteLine($"{(tcpClient.Client.RemoteEndPoint as IPEndPoint).Address}: {msg}");

                        ParseTCPRequest(msg, tcpClient);
                    }

                    

                    // Send a response back to the client.
                    //byte[] response = Encoding.ASCII.GetBytes($"You sent: {message}");
                    //networkStream.Write(response, 0, response.Length);

                    /*sw.Stop();
                    int timeToWait = (int) (((long)1000 / TCPTicks) - sw.ElapsedMilliseconds);
                    Console.WriteLine($"T: {timeToWait}\tct: {ct++}");
                    Thread.Sleep(timeToWait);*/
                }
                catch (Exception e)
                {
                    Console.WriteLine("TCP Error: " + e.Message);
                }
            }
                

            // Close the connection.
            tcpClient?.Close();
        }
        static void SendTCPMessage(string message, TcpClient tcpClient)
        {
            byte[] responseData = Encoding.ASCII.GetBytes(message + COM.Values.EOF);
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
                case "PING":
                    SendTCPMessage("PONG", tcpClient);
                    break;
                case "LIST":
                    SendLobbyList(tcpClient);
                    break;
                case "CREA":
                    CreateLobby(data, tcpClient);
                    break;
                case "JOIN":
                    JoinLobby(data, tcpClient);
                    break;
                case "LEAV":
                    LeaveLobby(data, tcpClient);
                    break;
                case "STRT":
                    StartLobby(data, tcpClient);
                    break;
                case "ENDG":
                    EndLobbyGame(data, tcpClient);
                    break;
                case "LNUP":
                    UpdateLobbyName(data, tcpClient);
                    break;
                case "FORW":
                    ForwardMessage(data, tcpClient);
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }

        private static void ForwardMessage(string message, TcpClient tcpClient)
        {
            if (message.Length < COM.Values.ClientIDLength) return;
            string senderClientID = message.Substring(0, COM.Values.ClientIDLength);
            string recipientID = recipients[senderClientID];
            var recipient = clients[recipientID];

            string response = "FORW" + message.Substring(COM.Values.ClientIDLength);
            Console.WriteLine($"Sending response: {response}");

            // Send a message to its recipient.
            SendTCPMessage(response, recipient);
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

                    var player = new Client(lobbyInfo.Player, clientIP);
                    var lobby = new Lobby(lobbyInfo.LobbyName, player);

                    AddClient(player.ID, tcpClient);

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

        private static void AddClient(string id, TcpClient tcpClient)
        {
            if (!clients.ContainsKey(id)) clients.Add(id, tcpClient);
            
            /*if (clients.ContainsKey(id))
            {
                clients[id]?.Dispose();
                clients[id] = tcpClient;
            }
            else clients.Add(id, tcpClient);*/
        }

        static void JoinLobby(string data, TcpClient tcpClient)
        {
            var lobbyInfo = JsonSerializer.Deserialize<COM.JoinLeaveLobbyInfo>(data);
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
                    var player = new Client(lobbyInfo.Player, clientIP);

                    AddClient(player.ID, tcpClient);

                    lobby.AddPlayerB(player);
                    Console.WriteLine($"{lobbyInfo.Player.Name} joined lobby '{lobby.Name}'.");

                    // Send confirmation
                    BroadcastLobbyUpdate(lobby);
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

        private static void LeaveLobby(string data, TcpClient tcpClient)
        {
            var lobbyInfo = JsonSerializer.Deserialize<COM.JoinLeaveLobbyInfo>(data);
            if (lobbyInfo == null) return;

            var lobbyID = lobbyInfo.LobbyID;
            if (lobbyID == "") return;

            lock (lobbiesLock)
            {
                List<Lobby> lobbiesFound = lobbies.Where(lobby => lobby.ID == lobbyID).ToList();

                if (lobbiesFound.Any())
                {
                    var lobby = lobbiesFound[0];
                    lobby.RemovePlayer(new Client(lobbyInfo.Player, ""));
                    if (lobby.PlayerA == null && lobby.PlayerB == null) lobbies.Remove(lobby);

                    Console.WriteLine($"{lobbyInfo.Player.Name} left lobby '{lobby.Name}'.");

                    // Send confirmation
                    BroadcastLobbyUpdate(lobby);
                }
                else
                {
                    Console.WriteLine($"Lobby '{lobbyID}' does not exist.");
                }
            }
        }

        private static void StartLobby(string data, TcpClient tcpClient)
        {
            var startGameInfo = JsonSerializer.Deserialize<COM.StartEndGameInfo>(data);
            if (startGameInfo == null) return;

            var lobbyID = startGameInfo.LobbyID;

            var lobbiesFound = lobbies.Where(l => l.ID == startGameInfo.LobbyID).ToList();

            if (lobbiesFound.Any())
            {
                var lobby = lobbiesFound[0];
                
                bool isLeader = false;
                foreach (var player in new List<COM.Client>() { lobby.PlayerA, lobby.PlayerB })
                {
                    if (player.ID == startGameInfo.PlayerID && player.LobbyLeader) isLeader = true;
                } 

                if (isLeader)
                {
                    //add client recipient bind
                    try
                    {
                        // Bind A to B
                        IPEndPoint remoteEP = new IPEndPoint((clients[lobby.PlayerB.ID].Client.RemoteEndPoint as IPEndPoint).Address, lobby.PlayerB.UdpPort);
                        recipients.Add(lobby.PlayerA.ID, lobby.PlayerB.ID);
                        udpRecipients.Add(lobby.PlayerA.ID, remoteEP);
                        // Bind B to A
                        remoteEP = new IPEndPoint((clients[lobby.PlayerA.ID].Client.RemoteEndPoint as IPEndPoint).Address, lobby.PlayerA.UdpPort);
                        recipients.Add(lobby.PlayerB.ID, lobby.PlayerA.ID);
                        udpRecipients.Add(lobby.PlayerB.ID, remoteEP);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error on UDP binding: {e}");
                        return;
                    }

                    Console.WriteLine($"Starting lobby '{lobby.Name}'.");

                    lobby.CurrentGameState = GameState.InGame;
                    BroadcastLobbyUpdate(lobby);
                }
            }
            else
            {
                Console.WriteLine($"Lobby '{lobbyID}' does not exist.");
            }
        }

        private static void EndLobbyGame(string data, TcpClient tcpClient)
        {
            var endGameInfo = JsonSerializer.Deserialize<COM.StartEndGameInfo>(data);
            if (endGameInfo == null) return;

            var lobbyID = endGameInfo.LobbyID;

            var lobbiesFound = lobbies.Where(l => l.ID == endGameInfo.LobbyID).ToList();

            if (lobbiesFound.Any())
            {
                var lobby = lobbiesFound[0];
                lobby.CurrentGameState = GameState.EndGame;
                BroadcastLobbyUpdate(lobby);
                lobbies.Remove(lobby);
            }
            else
            {
                Console.WriteLine($"Lobby '{lobbyID}' does not exist.");
            }
        }


        private static void UpdateLobbyName(string data, TcpClient tcpClient)
        {
            var lobbyNameInfo = JsonSerializer.Deserialize<COM.LobbyNameUpdateInfo>(data);
            if (lobbyNameInfo == null) return;

            var lobbyID = lobbyNameInfo.LobbyID;

            var lobbiesFound = lobbies.Where(l => l.ID == lobbyNameInfo.LobbyID).ToList();

            if (lobbiesFound.Any())
            {
                var lobby = lobbiesFound[0];

                bool isLeader = false;
                foreach (var player in new List<COM.Client>() { lobby.PlayerA, lobby.PlayerB })
                {
                    if (player != null && player.ID == lobbyNameInfo.PlayerID && player.LobbyLeader) isLeader = true;
                }

                if (isLeader)
                {
                    lobby.Name = lobbyNameInfo.NewName;
                    BroadcastLobbyUpdate(lobby);
                }
            }
            else
            {
                Console.WriteLine($"Lobby '{lobbyID}' does not exist.");
            }
        }

        private static void BroadcastLobbyUpdate(Lobby lobby)
        {
            foreach (COM.Client client in new List<COM.Client>() { lobby.PlayerA, lobby.PlayerB })
            {
                if (client != null)
                {
                    if (clients.ContainsKey(client.ID))
                    {
                        string jsonLobby = JsonSerializer.Serialize(lobby as COM.Lobby);
                        SendTCPMessage("LOUP" + jsonLobby, clients[client.ID]);
                    }
                }
            }
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

        /// ///////////////////////////////////////////////////////////////////////////////////////////
        
        /*
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
        }*/

        /// ///////////////////////////////////////////////////////////////////////////////////////////









        /*
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
        }*/
    }
}