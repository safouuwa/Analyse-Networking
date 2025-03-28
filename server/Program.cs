using System;
using System.Data;
using System.Data.SqlTypes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LibData;

// ReceiveFrom();
class Program
{
    static void Main(string[] args)
    {
        ServerUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}


class ServerUDP
{
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    // TODO: [Read the JSON file and return the list of DNSRecords]

    static List<DNSRecord>? dnsrecords = JsonSerializer.Deserialize<List<DNSRecord>>(File.ReadAllText(@"DNSrecords.json"));
    static byte[] buffer = new byte[1000];
    static string data = null;
    static int ackcount = 0;



    public static void start()
    {

        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var ipAddress = IPAddress.Parse(setting.ServerIPAddress);
        IPEndPoint ep1 = new IPEndPoint(ipAddress, setting.ServerPortNumber);
        socket.Bind(ep1);

        while (true)
        {
            // TODO:[Receive and print Hello]
            EndPoint clientep = new IPEndPoint(IPAddress.Any, 0);;
            int b = socket.ReceiveFrom(buffer, ref clientep);
            data = Encoding.ASCII.GetString(buffer, 0, b);
            Message dnsmsg = JsonSerializer.Deserialize<Message>(data);
            Console.WriteLine("Received from client: " + dnsmsg.Content);
            if (dnsmsg.MsgType == MessageType.Hello)
            {
                // TODO:[Send Welcome to the client]
                HelloReply(socket, clientep);
            }
            else if (dnsmsg.MsgType == MessageType.DNSLookup)
            {
                // TODO:[Receive and print DNSLookup]
                DNSLookupReply(socket, clientep, dnsmsg);
            }
            else if (dnsmsg.MsgType == MessageType.Ack)
            {
                // TODO:[Receive Ack about correct DNSLookupReply from the client]
                // TODO:[If no further requests receieved send End to the client]
                var v = AcknowledgementHandle(socket, clientep, dnsmsg);
                if (!v) break;
            }
        }
    }



    public static void HelloReply(Socket socket, EndPoint ep)
    {
        var welcome = new Message
        {
            MsgId = 2,
            MsgType = MessageType.Welcome,
            Content = "Welcome from server"
        };
        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(welcome));
        Console.WriteLine("Reply to client: " + welcome.Content);
        socket.SendTo(msg, ep);
    }

    public static void DNSLookupReply(Socket socket, EndPoint ep, Message dnsmsg)
    {
        var content1 = dnsmsg.Content as JsonElement?;
        var rec1 = JsonSerializer.Deserialize<DNSRecord>(content1.Value.GetRawText());
        var foundmsg1 = dnsrecords.Find(x => x.Name == rec1.Name && x.Type == rec1.Type);
        Message dnsreply1;

        if (foundmsg1 != null)
        {
            dnsreply1 = new Message
            {
                MsgId = dnsmsg.MsgId,
                MsgType = MessageType.DNSLookupReply,
                Content = foundmsg1
            };
        }
        else
        {
            dnsreply1 = new Message
            {
                MsgId = dnsmsg.MsgId,
                MsgType = MessageType.Error,
                Content = "DNS record not found"
            };
        }

        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(dnsreply1));
        Console.WriteLine("Reply to client: " + JsonSerializer.Serialize(dnsreply1.Content));
        socket.SendTo(msg, ep);
    }

    public static bool AcknowledgementHandle(Socket socket, EndPoint ep, Message dnsmsg)
    {
        ackcount++;
        Console.WriteLine("Acknowledgement received from client!");

        if (ackcount == 4) // Assuming 4 DNSLookup requests
        {
            var endMessage = new Message
            {
                MsgId = 9999,
                MsgType = MessageType.End,
                Content = "End of communication"
            };
            var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(endMessage));
            Console.WriteLine("Reply to client: " + endMessage.Content);
            socket.SendTo(msg, ep);
            return false;
        }
        return true;
    }
}