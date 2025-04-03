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
        //TODO: [Receive and print Welcome from server]
        IPEndPoint serverep = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        EndPoint serverend = serverep;
        SendHello(socket, serverend);
        var welcome = Listen(socket, serverend);
        var msgwelcome = welcome.Content as JsonElement?;
        Console.WriteLine("Received from server: " + msgwelcome);
        
        // TODO: [Create and send DNSLookup Message]
        //TODO: [Receive and print DNSLookupReply from server]
        //TODO: [Send Acknowledgment to Server]
        // TODO: [Send next DNSLookup to server]
        var dnsLookups = new List<Message>
        {
            new Message { MsgId = 33, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.outlook.com" } },
            new Message { MsgId = 34, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.nonexistent.com" } },
            new Message { MsgId = 35, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.sample.com" } },
            new Message { MsgId = 36, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.fake.com" } }
        };
        foreach (var dnsLookup in dnsLookups)
        {
            SendDNSLookup(socket, serverend, dnsLookup);
            var repl = Listen(socket, serverend);
            Console.WriteLine($"Received from server: {repl.Content as JsonElement?}");
            var ack1 = new Message
            {
                MsgId = dnsLookup.MsgId + 1000,
                MsgType = MessageType.Ack,
                Content = dnsLookup.MsgId.ToString()
            };
            SendAck(socket, serverend, ack1);
        }
        //TODO: [Receive and print End from server]
        var end = Listen(socket, serverend);
        var msgend = end.Content as JsonElement?;
        if (end.MsgType == MessageType.End)
        {
            Console.WriteLine("Received from server: " + msgend);
            socket.Close();
        }
    }

    public static void SendHello(Socket socket, EndPoint ep)
    {
        var hello = new Message
        {
            MsgId = 1,
            MsgType = MessageType.Hello,
            Content = "Hello from client"
        };
        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(hello));
        var content = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(hello.Content));
        Console.WriteLine("Message to server: " + content);
        socket.SendTo(msg, ep);
    }

    public static Message Listen(Socket socket, EndPoint ep)
    {
        int b = socket.ReceiveFrom(buffer, ref ep);
        data = Encoding.ASCII.GetString(buffer, 0, b);
        Message dnsmsg = JsonSerializer.Deserialize<Message>(data);
        return dnsmsg;
    }

    public static void SendDNSLookup(Socket socket, EndPoint ep, Message dnsLookup)
    {
        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(dnsLookup));
        var content = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(dnsLookup.Content));
        Console.WriteLine("Message to server: " + content);
        socket.SendTo(msg, ep);
    }
    public static void SendAck(Socket socket, EndPoint ep, Message ackmsg)
    {
        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(ackmsg));
        var content = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(ackmsg.Content));
        Console.WriteLine("Acknowledgment to server for Message ID:" + content);
        socket.SendTo(msg, ep);
    }
}