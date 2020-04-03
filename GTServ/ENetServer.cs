using System;
using System.Collections.Generic;
using System.Net;
using ENet.Managed;

namespace GTServ
{
    public class ENetServer
    {
        public Guid InstanceId { get; }
        
        private readonly ENetHost _host;
        
        public List<ENetPeer> Peers = new List<ENetPeer>();
        public List<World> Worlds = new List<World>();

        public ENetServer(string hostname, ushort port = 17091)
        {
            ManagedENet.Startup();
            
            var address = new IPEndPoint(IPAddress.Any, port);
            if (hostname != "0.0.0.0")
            {
                var addresses = Dns.GetHostAddresses(hostname);
                if (addresses.Length == 0)
                    throw new ArgumentException("Unable to retrieve address from specified host name.", nameof(hostname));
            
                address = new IPEndPoint(addresses[0], port);
            }

            _host = new ENetHost(address, 1024, 10);
            _host.ChecksumWithCRC32();
            _host.CompressWithRangeCoder();
            
            InstanceId = Guid.NewGuid();

            OnConnect += (sender, args) =>
            {
                Console.WriteLine("A new Peer tries to connect!");
                lock (Peers)
                    Peers.Add(args.Peer);
                                
                var player = new Player(args.Peer);
                player.OnConnect();
                
                args.Peer.OnDisconnect += (o, u) =>
                {
                    lock (Peers)
                        Peers.Remove(o as ENetPeer);
                    
                    player.OnDisconnect();
                };
            };
            
            _host.OnConnect += OnConnect;
        }

        public void LoadWorld(World world)
        {
            world.CurrentServer = this;
            Worlds.Add(world);
        }

        public EventHandler<ENetConnectEventArgs> OnConnect;

        public int PeerCount => _host.ConnectedPeers;

        public void Broadcast(byte[] data) => _host.Broadcast(data, 0, ENetPacketFlags.Reliable);
        
        public void Run() =>_host.StartServiceThread();
        public void Stop() => _host.StopServiceThread();
    }
}