using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ENet.Managed;
using Microsoft.Extensions.Logging;

namespace GTServ.Pool
{
    public class ServerPool : IDisposable
    {
        public static ServerPool Instance { get; private set; }
        
        private readonly ILogger<ServerPool> _logger;
        
        // TODO: Move this into a Config File!
        private readonly string _hostName = "0.0.0.0";
        private ushort _lastPort = 17092;
        
        private ConcurrentDictionary<Guid, ENetServer> _ENetServers
            = new ConcurrentDictionary<Guid, ENetServer>();

        public List<ENetPeer> AllPeers => _ENetServers.Select(x => x.Value).SelectMany(x => x.Peers).ToList();
        public List<Player> AllPlayers => _ENetServers.Select(x => x.Value).SelectMany(x => x.Peers).Select(x => x.Data as Player).ToList();

        public ServerPool(ILogger<ServerPool> logger)
        {
            _logger = logger;
            Instance = this;
        }

        public ENetServer GetBalancedServer()
        {
            var servers = _ENetServers
                .Where(x => x.Value.PeerCount <= 1024 && x.Value.Worlds.Count <= 1024 / 45) // a minimum of 45 players per world.
                .ToList();
            
            return servers.Any() ?
                servers.First().Value :
                StartupInstance();
        }
        
        public void Broadcast(byte[] data) // Broadcast to All Servers at Once
        {
            foreach (var srv in _ENetServers)
                srv.Value.Broadcast(data);
        }

        public void Collect()
        {
            var toShutDown = (from s in _ENetServers where s.Value.PeerCount <= 0 select s.Value).ToList();

            foreach (var s in toShutDown)
                ShutdownInstance(s);
            
            GC.Collect(); // Let also collect Garbage at this point.
        }

        public ENetServer StartupInstance() => StartupInstance(new ENetServer(_hostName, _lastPort++));
        
        public ENetServer StartupInstance(ENetServer instance)
        {
            Console.WriteLine($"Starting up Instance {instance.InstanceId}");
            _logger.LogInformation("Starting up InstanceId {instance}", instance.InstanceId);
            
            _ENetServers[instance.InstanceId] = instance;
            instance.Run();
            return instance;
        }

        public void ShutdownInstance(ENetServer instance)
            => ShutdownInstance(instance.InstanceId);
        
        public void ShutdownInstance(Guid instanceId)
        {
            Console.WriteLine($"Starting down Instance {instanceId}");
            _logger.LogInformation("Shutting down InstanceId {instance}", instanceId);
            
            if (!_ENetServers.TryRemove(instanceId, out var inst))
                throw new Exception($"Failed to shutdown Instance! {instanceId}");
            
            inst.Stop();
        }

        public void Dispose()
        {
            var toShutDown = (from s in _ENetServers select s.Value).ToList();

            foreach (var instance in toShutDown)
                ShutdownInstance(instance);
        }
    }
}