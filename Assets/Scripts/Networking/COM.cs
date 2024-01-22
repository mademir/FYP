using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using UnityEngine;
using System.Threading;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager.Requests;
using Newtonsoft.Json;

public class COM : MonoBehaviour
{
    public string IP = "127.0.0.1";
    TcpClient tcpClient;
    UdpClient udpClient;
    public int tcpBufferSize = 256;
    public int tcpPort = 1025;
    public int udpPort = 1026;

    public bool hosting = false;
    public bool send = false;
    public string msg = "";

    /* TODO:
     * Lobby communication. Receive Pair info (IP) and if hosting or not.
     * After receiving this, load main scene and connect udp to pair GO location etc.
     */


    private void Start()
    {
        
        try
        {
            tcpClient = new TcpClient(IP, tcpPort);
            Debug.Log($"Connected to TCP server at {IP}:{tcpPort}");
        }
        catch (Exception e)
        {
            Debug.Log("TCP Connection Error: " + e.Message);
        }

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
        if (send)
        {
            send = false;
            new Thread(() => SendTCPMessage(msg)).Start();
        }
    }


    void StartTCPServer(int port)
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

    void HandleTCPClient(TcpClient tcpClient)
    {
        try
        {
            string message = "";
            while (message != "exit")
            {
                NetworkStream networkStream = tcpClient.GetStream();
                byte[] data = new byte[tcpBufferSize];

                int bytesRead = networkStream.Read(data, 0, data.Length);
                message = Encoding.ASCII.GetString(data, 0, bytesRead);
                Console.WriteLine($"TCP Received: {message}");

                // Send a response back to the client.
                byte[] response = Encoding.ASCII.GetBytes($"You sent: {message}");
                networkStream.Write(response, 0, response.Length);
            }

            // Close the connection.
            tcpClient.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("TCP Error: " + e.Message);
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

            while (true)
            {
                byte[] data = new byte[tcpBufferSize];
                int bytesRead = networkStream.Read(data, 0, data.Length);
                string receivedMessage = Encoding.ASCII.GetString(data, 0, bytesRead);
                HandleTCPMessage(receivedMessage);
            }
        }
        catch (Exception e)
        {
            Debug.Log("TCP Receive Error: " + e.Message);
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
            default:
                Console.WriteLine($"Unknown command: {command}");
                break;
        }
    }

    private void ReceiveLobbyList(string data)
    {
        //List<string> lobbies = JsonConvert.DeserializeObject<List<string>>(data);

        //foreach (string lobby in lobbies) Debug.Log(lobby);
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
}
