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


    public static void start()
    {

        //TODO: [Create endpoints and socket]
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var ipAddress = IPAddress.Parse(setting.ClientIPAddress);
        IPEndPoint ep1 = new IPEndPoint(ipAddress, setting.ClientPortNumber);
        socket.Bind(ep1);

        //TODO: [Create and send HELLO]
        //TODO: [Receive and print Welcome from server]
        byte[] buffer = new byte[1000];
        byte[] msg = new byte[1000];
        string data = null;
        IPEndPoint serverep = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        EndPoint serverend = serverep;
        var hello = new Message
        {
            MsgId = 1,
            MsgType = MessageType.Hello,
            Content = "Hello from client"
        };
        msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(hello));
        if( hello != null )
        {
            socket.SendTo(msg, serverend);
            int be = socket.ReceiveFrom(buffer, ref serverend);
            data = Encoding.ASCII.GetString(buffer, 0, be);

            var welcome = JsonSerializer.Deserialize<Message>(data);
            var msgwelcome = welcome.Content as JsonElement?;
            Console.WriteLine("Received from server: " + msgwelcome);
            data = null;
        }

        // TODO: [Create and send DNSLookup Message]

        //TODO: [Receive and print DNSLookupReply from server]

        //TODO: [Send Acknowledgment to Server]

        // TODO: [Send next DNSLookup to server]
        // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply
        var dnsLookups = new List<Message>
        {
            new Message { MsgId = 33, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.outlook.com" } },
            new Message { MsgId = 34, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.nonexistent.com" } },
            new Message { MsgId = 35, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.sample.com" } },
            new Message { MsgId = 36, MsgType = MessageType.DNSLookup, Content = new { Type = "A", Name = "www.fake.com" } }
        };

        foreach (var dnsLookup in dnsLookups)
        {
            msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(dnsLookup));
            var content = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(dnsLookup.Content));
            Console.WriteLine("Message to server: " + content);
            socket.SendTo(msg, serverend);

            var b2 = socket.ReceiveFrom(buffer, ref serverend);
            data = Encoding.ASCII.GetString(buffer, 0, b2);
            var repl = JsonSerializer.Deserialize<Message>(data);
            Console.WriteLine($"Received from server: {repl.Content as JsonElement?}");

            var ack1 = new Message
            {
                MsgId = dnsLookup.MsgId + 1000,
                MsgType = MessageType.Ack,
                Content = dnsLookup.MsgId.ToString()
            };
            msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(ack1));
            content = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(ack1.Content));
            Console.WriteLine("Acknowledgment to server for Message ID:" + content);
            socket.SendTo(msg, serverend);
        }

        //TODO: [Receive and print End from server]
        var b = socket.ReceiveFrom(buffer, ref serverend);
        data = Encoding.ASCII.GetString(buffer, 0, b);
        var end = JsonSerializer.Deserialize<Message>(data);
        var msgend = end.Content as JsonElement?;
        if (end.MsgType == MessageType.End)
        {
            Console.WriteLine("" + msgend);
            socket.Close();
        }
    }
}