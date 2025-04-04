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
        Console.WriteLine("Server started and waiting for messages...");
        while (true)
        {
            // TODO:[Receive and print Hello]
            EndPoint clientep = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);;
            Message dnsmsg = Listen(socket, clientep);
            if (dnsmsg == null) continue;
            if (dnsmsg.MsgType == MessageType.Hello)
            {
                // TODO:[Send Welcome to the client] 
                Console.WriteLine("Received from client: " + dnsmsg.Content);
                HelloReply(socket, clientep);
            }
            else if (dnsmsg.MsgType == MessageType.DNSLookup)
            {
                // TODO:[Receive and print DNSLookup]
                Console.WriteLine("Received from client: " + dnsmsg.Content);
                DNSLookupReply(socket, clientep, dnsmsg);
            }
            else if (dnsmsg.MsgType == MessageType.Ack)
            {
                // TODO:[Receive Ack about correct DNSLookupReply from the client]
                Console.WriteLine("Received Acknowledgement from client for Message ID: " + dnsmsg.Content);
                AcknowledgementHandle(socket, clientep);
            }
            else SendError(socket, clientep, "Error: Unable to deserialize message; Server does not support the given Message Type.");
        }
    }

    public static Message Listen(Socket socket, EndPoint ep)
    {
        try
        {
            int b = socket.ReceiveFrom(buffer, ref ep);
            data = Encoding.ASCII.GetString(buffer, 0, b);
            Message dnsmsg = JsonSerializer.Deserialize<Message>(data);
            if (dnsmsg == null)
            {
                SendError(socket, ep, "Error: Unable to deserialize message; Message does not match the expected object format.");
                return null;
            }  
            return dnsmsg;
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error while setting listening for messages: {ex.Message}");
            SendError(socket, ep, "Error: Unable to receive message; Socket error occurred.", 9999);
            return null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON error while deserializing message: {ex.Message}");
            SendError(socket, ep, "Error: Unable to deserialize message; Message does not match the expected JSON format.", 9999);
            return null;
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
        socket.SendTo(msg, ep);
        Console.WriteLine("Reply to client: " + welcome.Content);
    }

    public static void DNSLookupReply(Socket socket, EndPoint ep, Message dnsmsg)
    {
        var content1 = dnsmsg.Content as JsonElement?;
        DNSRecord rec1;
        try
        {
            rec1 = JsonSerializer.Deserialize<DNSRecord>(content1.Value.GetRawText());
        }
        catch (JsonException ex)
        {
            SendError(socket, ep, "Error: Unable to deserialize message; DNSLookup message content does not match the expected object format.");
            return;
        }
        if (rec1 == null || string.IsNullOrEmpty(rec1.Name) || string.IsNullOrEmpty(rec1.Type))
        {
            SendError(socket, ep, "Error: Unable to deserialize message; DNSLookup message content is null or incomplete.");
            return;
        }
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
                Content = "Domain not found"
            };
        }

        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(dnsreply1));
        Console.WriteLine("Reply to client: " + JsonSerializer.Serialize(dnsreply1.Content));
        socket.SendTo(msg, ep);
    }

    public static bool AcknowledgementHandle(Socket socket, EndPoint ep)
    {
        ackcount++;
        // TODO:[If no further requests receieved send End to the client]
        if (ackcount == 4)
        {
            var endMessage = new Message
            {
                MsgId = 9999,
                MsgType = MessageType.End,
                Content = "End of DNSLookup"
            };
            var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(endMessage));
            Console.WriteLine("Reply to client: " + endMessage.Content);
            socket.SendTo(msg, ep);
            ackcount = 0;
            return false;
        }
        return true;
    }

    public static void SendError(Socket socket, EndPoint ep, string errormsg, int msgid = 8888)
    {
        var errorMessage = new Message
        {
            MsgId = msgid,
            MsgType = MessageType.Error,
            Content = errormsg
        };
        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(errorMessage));
        Console.WriteLine("Reply to client: " + errorMessage.Content);
        socket.SendTo(msg, ep);
    }
}