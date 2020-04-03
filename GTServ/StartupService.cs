using System;
using System.Numerics;
using System.Threading.Tasks;
using GTServ.Pool;
using GTServ.RTSoft;
using Microsoft.Extensions.Logging;

namespace GTServ
{
    public class StartupService
    {
        private readonly ILogger<StartupService> _logger;
        private readonly ServerPool _serverPool;
        private readonly ItemDatabase _idb;
        private readonly EventManager _eventManager;
        private readonly WorldManager _worldManager;

        public StartupService(ILogger<StartupService> logger, ServerPool serverPool, ItemDatabase idb, EventManager eventManager,
            WorldManager worldManager)
        {
            _logger = logger;
            _serverPool = serverPool;
            _idb = idb;
            _eventManager = eventManager;
            _worldManager = worldManager;
        }
        
        public async Task Run()
        {
            _idb.Pack(); // Lets pre-package all the Items.
            
            Console.WriteLine("Spinning up some Servers onto the Server Pool!");
            
            _logger.LogInformation("Spinning up some Servers onto the Server Pool!");
            for (var i = 0; i < 1; i++)
                _serverPool.StartupInstance();
            

            await Task.Delay(-1);
        }
    }
}