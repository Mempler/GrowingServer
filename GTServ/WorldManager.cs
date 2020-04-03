using System.Collections.Concurrent;
using GTServ.Pool;

namespace GTServ
{
    public class WorldManager
    {
        public ConcurrentDictionary<string, World> ActiveWorlds { get; }
            = new ConcurrentDictionary<string, World>();
        
        public static WorldManager Instance { get; private set; }

        public WorldManager()
        {
            Instance = this;
        }

        public World Load(string name)
        {
            if (ActiveWorlds.ContainsKey(name))
                return ActiveWorlds[name];

            var world = World.Load(name);
            var server = ServerPool.Instance.GetBalancedServer();
            server.LoadWorld(world);

            return world;
        }
    }
}