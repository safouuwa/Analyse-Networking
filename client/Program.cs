using System.Collections.Immutable;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibData;

// SendTo();
class Program
{
    static void Main(string[] args)
    {
        ClientUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}

class ClientUDP
{

    //TODO: [Deserialize Setting.json]
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);
    static byte[] buffer = new byte[1000];
    static string data = null;


    public static void start()
    {

        //TODO: [Create endpoints and socket]
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var ipAddress = IPAddress.Parse(setting.ClientIPAddress);
        IPEndPoint ep1 = new IPEndPoint(ipAddress, setting.ClientPortNumber);
        socket.Bind(ep1);

        //TODO: [Create and send HELLO]
        IPEndPoint serverep = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        EndPoint serverend = serverep;
        if (!SendHello(socket, serverend))
        {
            Console.WriteLine("Error: Unable to send Hello message.");
            Console.WriteLine("Closing client socket...");
            socket.Close();
            return;
        }
        //TODO: [Receive and print Welcome from server]
        var welcome = Listen(socket, serverend);
        if (welcome == null) return;
        
        // TODO: [Create and send DNSLookup Message]
        var dnsLookups = new List<Message>
        {
            new Message { MsgId = 33, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.outlook.com" } },
            new Message { MsgId = 34, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.nonexistent.com" } },
            new Message { MsgId = 35, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.sample.com" } },
            new Message { MsgId = 36, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.fake.com" } }
        };
        // TODO: [Send next DNSLookup to server]
        foreach (var dnsLookup in dnsLookups)
        {
            SendMessage(socket, serverend, dnsLookup);
            //TODO: [Receive and print DNSLookupReply from server]
            var repl = Listen(socket, serverend);
            if (repl == null) return;
            //TODO: [Send Acknowledgment to Server]
            var ack1 = new Message
            {
                MsgId = dnsLookup.MsgId + 1000,
                MsgType = MessageType.Ack,
                Content = dnsLookup.MsgId.ToString()
            };
            SendMessage(socket, serverend, ack1);
        }
        //TODO: [Receive and print End from server]
        var end = Listen(socket, serverend);
        if (end == null) return;
        if (end.MsgType == MessageType.End) return;
    }

    public static bool SendHello(Socket socket, EndPoint ep)
    {
        var hello = new Message
        {
            MsgId = 1,
            MsgType = MessageType.Hello,
            Content = "Hello from client"
        };
        try
        {
            var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(hello));
            Console.WriteLine("Message to server: " + hello.Content);
            socket.SendTo(msg, ep);
            return true;
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error while sending Hello message: {ex.Message}");
            return false;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON error while serializing message: {ex.Message}");
            return false;
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
                Console.WriteLine("Error: Unable to deserialize message; Message does not match the expected object format.\nClosing client socket...");
                return null;
            }
            if (dnsmsg.MsgType == MessageType.End)
            {
                Console.WriteLine("Received from server: " + dnsmsg.Content);
                Console.WriteLine("Closing client socket...");
                socket.Close();
            }
            else if (dnsmsg.MsgType == MessageType.Error && dnsmsg.MsgId == 9999)
            {
                Console.WriteLine("Received from server: " + dnsmsg.Content);
                Console.WriteLine("Closing client socket...");
                socket.Close();
                return null;
            }
            else if (dnsmsg.MsgType == MessageType.Error)
            {
                Console.WriteLine("Received from server: " + dnsmsg.Content);
            }
            else if (dnsmsg.MsgType == MessageType.Welcome)
            {
                Console.WriteLine("Received from server: " + dnsmsg.Content);
            }
            return dnsmsg;
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error while setting listening for messages: {ex.Message}\nClosing client socket...");
            return null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON error while deserializing message: {ex.Message}\nClosing client socket...");
            return null;
        }
    }

    public static void SendMessage(Socket socket, EndPoint ep, Message msg1)
    {
        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg1));
        try
        {
            Console.WriteLine("Message to server: " + msg1.MsgType +" "+ msg1.Content.ToString());
            socket.SendTo(msg, ep);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error while sending message: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON error while serializing message: {ex.Message}");
        }
    }
}