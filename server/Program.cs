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
            if (dnsmsg == null) continue; // als de message null/niet correct is, dan wordt er niets gedaan
            if (dnsmsg.MsgType == MessageType.Hello) //als de message type een Hello is, dan wordt er een Welcome message verstuurd naar de client
            {
                // TODO:[Send Welcome to the client] 
                Console.WriteLine("Received from client: " + dnsmsg.Content);
                HelloReply(socket, clientep);
            }
            else if (dnsmsg.MsgType == MessageType.DNSLookup) //als de message type een DNSLookup is, dan wordt er een DNSLookupReply message verstuurd naar de client
            {
                // TODO:[Receive and print DNSLookup]
                Console.WriteLine("Received from client: " + dnsmsg.Content);
                DNSLookupReply(socket, clientep, dnsmsg);
            }
            else if (dnsmsg.MsgType == MessageType.Ack) //als de message type een acknowledgement is, dan wordt er hier een acknowledgement ontvangen
            {
                // TODO:[Receive Ack about correct DNSLookupReply from the client]
                Console.WriteLine("Received Acknowledgement from client for Message ID: " + dnsmsg.Content);
                AcknowledgementHandle(socket, clientep);
            }
            else SendError(socket, clientep, "Error: Unable to deserialize message; Server does not support the given Message Type."); //als de message type niet wordt herkend, dan wordt er een error message verstuurd naar de client
        }
    }

    public static Message Listen(Socket socket, EndPoint ep) //method voor het luisteren naar berichten van de client
    {
        try
        {
            int b = socket.ReceiveFrom(buffer, ref ep);
            data = Encoding.ASCII.GetString(buffer, 0, b);
            Message dnsmsg = JsonSerializer.Deserialize<Message>(data);
            if (dnsmsg == null)
            {
                Console.WriteLine("Received from client: Error: Unable to deserialize message; Message does not match the expected object format.");
                SendError(socket, ep, "Error: Unable to deserialize message; Message does not match the expected object format.");
                return null;
            }  
            return dnsmsg;
        }
        catch (SocketException ex) //exception handling voor socket errors (voornamelijk als de client niet bereikbaar is)
        {
            Console.WriteLine($"Received from client: Socket error while setting listening for messages: {ex.Message}");
            SendError(socket, ep, "Error: Unable to receive message; Socket error occurred.", 9999);
            return null;
        }
        catch (JsonException ex) //exception handling voor JSON errors (voornamelijk als de client niet bereikbaar is)
        {
            Console.WriteLine($"Received from client: JSON error while deserializing message: {ex.Message}");
            SendError(socket, ep, "Error: Unable to deserialize message; Message does not match the expected JSON format.");
            return null;
        }
    }

    public static void HelloReply(Socket socket, EndPoint ep) //method voor het afhandelen van een hello message van de client
    {
        var welcome = new Message
        {
            MsgId = 2,
            MsgType = MessageType.Welcome,
            Content = "Welcome from server"
        };
        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(welcome));
        try
        {
            socket.SendTo(msg, ep);
        }
        catch (SocketException ex) //exception handling voor socket errors (voornamelijk als de client niet bereikbaar is)
        {
            Console.WriteLine($"Socket error while sending message: {ex.Message}");
            return;
        }
        Console.WriteLine("Reply to client: " + welcome.Content);
    }

    public static void DNSLookupReply(Socket socket, EndPoint ep, Message dnsmsg) //method voor het afhandelen van een DNSLookup message van de client
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

        if (foundmsg1 != null) //als de DNS record is gevonden, dan wordt er een reply message gemaakt met de gevonden record
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
        try
        {
            socket.SendTo(msg, ep);
        }
        catch (SocketException ex) //exception handling voor socket errors (voornamelijk als de client niet bereikbaar is)
        {
            Console.WriteLine($"Socket error while sending message: {ex.Message}");
            return;
        }
    }

    public static void AcknowledgementHandle(Socket socket, EndPoint ep) //method voor het afhandelen van een acknowledgement message van de client
    {
        ackcount++;
        // TODO:[If no further requests receieved send End to the client]
        if (ackcount == 4) //als er 4 acknowledgements zijn ontvangen, dan wordt er een end message verstuurd naar de client
        {
            var endMessage = new Message
            {
                MsgId = 9999,
                MsgType = MessageType.End,
                Content = "End of DNSLookup"
            };
            var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(endMessage));
            Console.WriteLine("Reply to client: " + endMessage.Content);
            try
            {
                socket.SendTo(msg, ep);
            }
            catch (SocketException ex) //exception handling voor socket errors (voornamelijk als de client niet bereikbaar is)
            {
                Console.WriteLine($"Socket error while sending message: {ex.Message}");
                return;
            }
            ackcount = 0;
            return;
        }
        return;
    }

    public static void SendError(Socket socket, EndPoint ep, string errormsg, int msgid = 8888) //method voor het versturen van error messages naar de client, id 8888 houdt in dat de client erna niet gesloten hoeft te worden
    {
        var errorMessage = new Message //error message om te versturen naar de server
        {
            MsgId = msgid,
            MsgType = MessageType.Error,
            Content = errormsg
        };
        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(errorMessage));
        Console.WriteLine("Reply to client: " + errorMessage.Content);
        try
        {
            socket.SendTo(msg, ep);
        }
        catch (SocketException ex) //exception handling voor socket errors (voornamelijk als de client niet bereikbaar is)
        {
            Console.WriteLine($"Socket error while sending message: {ex.Message}");
            return;
        }
    }
}