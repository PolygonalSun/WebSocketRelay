using Fleck;
using System;
using System.Collections.Concurrent;

namespace WebSocketRelay
{
    class Program
    {
        private static string GetConnectionString(IWebSocketConnection socket)
        {
            return socket.ConnectionInfo.ClientIpAddress + ":" + socket.ConnectionInfo.ClientPort;
        }

        static void Main(string[] args)
        {
            string broadcasterPort = "7602";
            string receiverPort = "7603";

            if (args.Length == 2)
            {
                broadcasterPort = args[0];
                receiverPort = args[1];
            }
            else if (args.Length > 0)
            {
                Console.WriteLine("Unrecognized arguments:\n\t" + string.Join(' ', args));
                Console.WriteLine("Correct usage:\n\tWebSocketRelay.exe [broadcasterPort receiverPort]");
                return;
            }

            var receivers = new ConcurrentDictionary<string, IWebSocketConnection>();

            var broadcasterServer = new WebSocketServer("ws://127.0.0.1:" + broadcasterPort);
            broadcasterServer.Start(socket =>
            {
                socket.OnOpen = () => Console.WriteLine("Opened broadcaster connection with " + GetConnectionString(socket));
                socket.OnClose = () => Console.WriteLine("Closed broadcaster connection with " + GetConnectionString(socket));
                socket.OnMessage = message =>
                {
                    foreach (var receiver in receivers.Values)
                    {
                        receiver.Send(message);
                    }
                };
            });

            var receiverServer = new WebSocketServer("ws://127.0.0.1:" + receiverPort);
            receiverServer.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    var connectionString = GetConnectionString(socket);
                    Console.WriteLine("Opened receiver connection with " + connectionString);
                    receivers[connectionString] = socket;
                };
                socket.OnClose = () =>
                {
                    var connectionString = GetConnectionString(socket);
                    Console.WriteLine("Closed receiver connection with " + connectionString);
                    if (!receivers.TryRemove(connectionString, out socket))
                    {
                        throw new Exception("Unable to remove receiver connection with " + connectionString);
                    }
                };
            });

            Console.WriteLine();
            Console.WriteLine("WebSocketRelay server is now running! Listening for broadcasters on port " + broadcasterPort +
                " and for receivers on port " + receiverPort + ". To stop server, use Ctrl+C or type \"exit\" into " +
                "the console and press Enter.");
            Console.WriteLine();
            while (!Console.ReadLine().StartsWith("exit")) ;
        }
    }
}
