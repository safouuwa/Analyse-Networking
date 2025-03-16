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
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var ipAddress = IPAddress.Parse(setting.ClientIPAddress);
        IPEndPoint ep1 = new IPEndPoint(ipAddress, setting.ClientPortNumber);
        socket.Bind(ep1);

        //TODO: [Create and send HELLO]
        //TODO: [Receive and print Welcome from server]
        byte[] buffer = new byte[1000];
        byte[] msg = new byte[1000];
        string data = null;
        IPEndPoint serverep = new IPEndPoint(ipAddress, setting.ServerPortNumber);
        ConsoleKeyInfo key;
        socket.Connect(serverep);
        while (true)
        {
            data = "HELLO";
            if( data.Length != 0 )
            {
                msg = Encoding.ASCII.GetBytes(data);
                socket.Send(msg);
                int b = socket.Receive(buffer);
                data = Encoding.ASCII.GetString(buffer, 0, b);

                Console.WriteLine("" + data);
                data = null;
                break;
            }

            Console.WriteLine("\n<< Continue 'y' , Exit 'e'>>\n");
            key = Console.ReadKey();
            if (key.KeyChar == 'e')
            {
                socket.Send(Encoding.ASCII.GetBytes("Closed"));
                Console.WriteLine("\nExiting.. Press any key to continue");
                key = Console.ReadKey();
                socket.Close();
                break;
            }

        }

        // TODO: [Create and send DNSLookup Message]


        //TODO: [Receive and print DNSLookupReply from server]


        //TODO: [Send Acknowledgment to Server]

        // TODO: [Send next DNSLookup to server]
        // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply

        //TODO: [Receive and print End from server]





    }
}