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



    public static void start()
    {


        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var ipAddress = IPAddress.Parse(setting.ServerIPAddress);
        IPEndPoint ep1 = new IPEndPoint(ipAddress, setting.ServerPortNumber);
        socket.Bind(ep1);


        // TODO:[Receive and print a received Message from the client]
        byte[] buffer = new byte[1000];
        var welcome = new Message
        {
            MsgId = 2,
            MsgType = MessageType.Welcome,
            Content = "Welcome from server"
        };
        var msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(welcome));
        string data = null;
        socket.Listen(5);
        Console.WriteLine("\n Waiting for clients..");

        // TODO:[Receive and print Hello]
        // TODO:[Send Welcome to the client]
        Socket newSock = socket.Accept();
        // TODO:[Receive and print DNSLookup]
        // TODO:[Query the DNSRecord in Json file]
        // TODO:[Receive Ack about correct DNSLookupReply from the client]
        // TODO:[If no further requests receieved send End to the client]
        var ackcount = 0;
        var content = new JsonElement();
        while (true)
        {
            int b = newSock.Receive(buffer);
            data = Encoding.ASCII.GetString(buffer, 0, b);
            Message dnsmsg = JsonSerializer.Deserialize<Message>(data);
            Console.WriteLine("Received from client: " + data);
            data = null;
            if (dnsmsg.MsgType == MessageType.Hello)
            {
                content = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(welcome));
                Console.WriteLine("Reply to client: " + content);
                newSock.Send(msg);
            }
            else if (dnsmsg.MsgType == MessageType.DNSLookup)
            {
                var content1 = dnsmsg.Content as JsonElement?;
                var rec1 = JsonSerializer.Deserialize<DNSRecord>(content1.Value.GetRawText());
                var foundmsg1 = dnsrecords.Find(x => x.Name == rec1.Name && x.Type == rec1.Type);
                Message dnsreply1;
                // TODO:[If found Send DNSLookupReply containing the DNSRecord]
                // TODO:[If not found Send Error]
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
                content = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(dnsreply1));
                Console.WriteLine("Reply to client: " + content);
                newSock.Send(Encoding.ASCII.GetBytes(JsonSerializer.Serialize(dnsreply1)));
            }
            else if (dnsmsg.MsgType == MessageType.Ack)
            {
                ackcount++;
                Console.WriteLine("Acknowledgement received from client!");

                if (ackcount == 4)
                {
                    var endMessage = new Message
                    {
                        MsgId = 9999,
                        MsgType = MessageType.End,
                        Content = "End of communication"
                    };
                    content = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(endMessage));
                    Console.WriteLine("Reply to client: " + content);
                    newSock.Send(Encoding.ASCII.GetBytes(JsonSerializer.Serialize(endMessage)));
                    break;
                }
            }
        }
        socket.Listen(5);
        Console.WriteLine("\n Waiting for clients..");
        socket.Accept();
    }
}