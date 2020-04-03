using System;
using System.IO;
using System.Net;
using System.Threading;
using ENet.Managed;

namespace GTServ.Proxy
{
    internal static class Program
    {
        public static ENetHost LocalServer; // The server which GT Connects With
        public static ENetHost RemoteServer; // The server which Proxy Connects with.
        public static ENetPeer RemotePeer;

        public static StreamWriter logFile = new StreamWriter(File.OpenWrite("log.txt"));
        
        public static IPEndPoint RemoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 17092);
        
        private static void Main(string[] args)
        {
            ManagedENet.Startup();
            
            Console.WriteLine("Setting up Local Server");
            LocalServer = new ENetHost(new IPEndPoint(IPAddress.Any, 17091), 1024, 10, 0, 0);
            LocalServer.ChecksumWithCRC32();
            LocalServer.CompressWithRangeCoder();

            LocalServer.OnConnect += (sender, eventArgs) =>
            {
                LogClient("Connected!");

                RemotePeer = RemoteServer.Connect(RemoteEndpoint, 1, 0);

                RemotePeer.OnReceive += (o, packet) =>
                {
                    SendToClient(packet);
                };
                
                eventArgs.Peer.OnReceive += (o, packet) =>
                {
                    SendToRemote(packet);
                };
                
                eventArgs.Peer.OnDisconnect += (o, u) =>
                {
                    RemotePeer.DisconnectNow(0);
                    LogClient("Disconnected!");
                };
            };
            
            LocalServer.StartServiceThread();
            
            Console.WriteLine("Setting up Remote Server");
            
            RemoteServer = new ENetHost(1, 10);
            RemoteServer.ChecksumWithCRC32();
            RemoteServer.CompressWithRangeCoder();

            RemoteServer.OnConnect += (sender, eventArgs) => LogServer("Connected!");

            RemoteServer.StartServiceThread();
            
            while (true) Thread.Sleep(5);
        }
        
        public static void SendToRemote(ENetPacket packet)
        {
            var cpy = packet.GetPayloadCopy();
            
            var dataHex = BitConverter.ToString(cpy).Replace("-", "");
            LogClient(dataHex);
            
            RemoteServer.Broadcast(cpy, 0, ENetPacketFlags.Reliable);
        }
        
        public static void SendToClient(ENetPacket packet)
        {
            var cpy = packet.GetPayloadCopy();

            var dataHex = BitConverter.ToString(cpy).Replace("-", "");
            LogServer(dataHex);
            
            LocalServer.Broadcast(cpy, 0, ENetPacketFlags.Reliable);
        }
        
        public static void LogClient(string d)
        {
            logFile.WriteLine($"C -> S: {d}");
            Console.WriteLine("C -> S: {0}", d);
        }
        
        public static void LogServer(string d)
        {
            logFile.WriteLine($"C <- S: {d}");
            Console.WriteLine("C <- S: {0}", d);
        }
    }
}