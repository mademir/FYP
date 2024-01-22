using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using UnityEngine;
using System.Threading;
using UnityEditor.Experimental.GraphView;

public class Client : MonoBehaviour
{
    public string IP = "127.0.0.1";
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


        /*// Connect to the TCP server
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
    }
}
