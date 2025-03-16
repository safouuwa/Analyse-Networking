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
        byte[] msg = Encoding.ASCII.GetBytes("WELCOME");
        string data = null;
        socket.Listen(5);
        Console.WriteLine("\n Waiting for clients..");
        



        // TODO:[Receive and print Hello]
        // TODO:[Send Welcome to the client]
        Socket newSock = socket.Accept();
        while (true)
        {
            int b = newSock.Receive(buffer);
            data = Encoding.ASCII.GetString(buffer, 0, b);
            if( data == "Closed")
            {
                newSock.Close();
                Console.WriteLine("Closing the socket..");
                break;
            }
            else
            {
                Console.WriteLine("" + data);
                data = null;
                newSock.Send(msg);
                break;
            }
        }
        socket.Close();


        // TODO:[Receive and print DNSLookup]


        // TODO:[Query the DNSRecord in Json file]

        // TODO:[If found Send DNSLookupReply containing the DNSRecord]



        // TODO:[If not found Send Error]


        // TODO:[Receive Ack about correct DNSLookupReply from the client]


        // TODO:[If no further requests receieved send End to the client]

    }


}