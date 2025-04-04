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
        var hello = new Message // hello message
        {
            MsgId = 1,
            MsgType = MessageType.Hello,
            Content = "Hello from client"
        };
        if (!SendMessage(socket, serverend, hello)) return;
        //TODO: [Receive and print Welcome from server]
        var welcome = Listen(socket, serverend);
        if (welcome != null && welcome.MsgType == MessageType.End || welcome == null) return;
        
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
            if (!SendMessage(socket, serverend, dnsLookup)) return;
            //TODO: [Receive and print DNSLookupReply from server]
            var repl = Listen(socket, serverend);
            if (repl != null && repl.MsgType == MessageType.End || repl == null) return;
            //TODO: [Send Acknowledgment to Server]
            var ack1 = new Message
            {
                MsgId = dnsLookup.MsgId + 1000,
                MsgType = MessageType.Ack,
                Content = dnsLookup.MsgId.ToString()
            };
            if (!SendMessage(socket, serverend, ack1)) return;
        }
        //TODO: [Receive and print End from server]
        var end = Listen(socket, serverend);
        if (end != null && end.MsgType == MessageType.End || end == null) return;
        Console.WriteLine("Client finished sending messages!");
    }


    public static Message Listen(Socket socket, EndPoint ep)
    {
        try //poging tot luisteren naar een message van de server
        {
            int b = socket.ReceiveFrom(buffer, ref ep);
            data = Encoding.ASCII.GetString(buffer, 0, b);
            Message dnsmsg = JsonSerializer.Deserialize<Message>(data);
            if (dnsmsg == null) //handling voor als de gekregen data niet in een message object kan worden omgezet
            {
                Console.WriteLine("Received from server: Error: Unable to deserialize message; Message does not match the expected object format.\nClosing client socket...");
                return null;
            }
            if (dnsmsg.MsgType == MessageType.End) //handling voor een end message van de server om de client af te sluiten
            {
                Console.WriteLine("Received from server: " + dnsmsg.Content);
                Console.WriteLine("Closing client socket...");
                socket.Close();
            }
            else if (dnsmsg.MsgType == MessageType.Error && dnsmsg.MsgId == 9999) //handling voor een error message van de server die aangeeft dat de client zou moeten sluiten (door een foute message van de client)
            {
                Console.WriteLine("Received from server: " + dnsmsg.Content);
                Console.WriteLine("Closing client socket...");
                socket.Close();
                return null;
            }
            else if (dnsmsg.MsgType == MessageType.Error) //handling voor een error message van het niet vinden van een lookup
            {
                Console.WriteLine("Received from server: " + dnsmsg.Content);
            }
            else if (dnsmsg.MsgType == MessageType.Welcome) //handling voor een welcome message van de server
            {
                Console.WriteLine("Received from server: " + dnsmsg.Content);
            }
            else if (dnsmsg.MsgType == MessageType.DNSLookupReply) //handling voor een DNSLookupReply message van de server
            {
                Console.WriteLine("Received from server: " + dnsmsg.Content);
            }
            else if (dnsmsg.MsgType == MessageType.Ack) //handling voor een acknowledgement message van de server
            {
                Console.WriteLine("Received from server: " + dnsmsg.Content);
            }
            else //handling voor een message type dat niet wordt herkend
            {
                Console.WriteLine("Received from server: Error: Unable to deserialize message; Client does not support the given Message Type.");
                return null;
            }
            return dnsmsg;
        }
        catch (SocketException ex) // exception handling voor socket errors (voornamelijk als de server niet bereikbaar is)
        {
            Console.WriteLine($"Socket error while setting listening for messages: {ex.Message}");
            Console.WriteLine("Closing client socket...");
            socket.Close();
            return null;
        }
        catch (JsonException ex) // exception handling voor json errors (voornamelijk als de message niet goed is geformatteerd)
        {
            Console.WriteLine($"Received from server: JSON error while deserializing message: {ex.Message}");
            return null;
        }
    }

    public static bool SendMessage(Socket socket, EndPoint ep, Message msg1) // method voor het versturen van een message naar de server
    {
        try
        {
            var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg1)); //de message wordt omgezet naar een byte array
            Console.WriteLine("Message to server: " + msg1.MsgType +": "+ msg1.Content.ToString());
            socket.SendTo(msg, ep);
            return true;
        }
        catch (SocketException ex) //exception handling voor socket errors (voornamelijk als de server niet bereikbaar is)
        {
            Console.WriteLine($"Socket error while sending message: {ex.Message}");
            Console.WriteLine("Closing client socket...");
            socket.Close();
            return false;
        }
        catch (JsonException ex) //exception handling voor json errors (voornamelijk als de message niet goed is geformatteerd)
        {
            Console.WriteLine($"JSON error while serializing own message: {ex.Message}");
            Console.WriteLine("Closing client socket...");
            socket.Close();
            return false;
        }
    }
}