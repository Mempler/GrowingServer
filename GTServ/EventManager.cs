using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GTServ.Events;
using ServiceProviderServiceExtensions = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions;

namespace GTServ
{
    public enum EventType
    {
        TextPacket
    }
    
    public class EventManager
    {
        private readonly IServiceProvider _provider;
        public static EventManager Instance { get; private set; }

        public EventManager(IServiceProvider provider)
        {
            _provider = provider;
            Instance = this;
        }
        
        public void RunEvent(EventType type, string eventName, Player sender, object data)
        {
            if (type == EventType.TextPacket)
            {
                var events = Assembly
                    .GetCallingAssembly()
                    .GetTypes()
                    .Where(x => typeof(ITextPacketEvent).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

                foreach (var e in events)
                {
                    var attr = e.GetCustomAttribute<TextPacketEventAttribute>();

                    if (attr.ActionType != eventName)
                        continue;

                    var ctor = e.GetConstructors()[0];
                    
                    var prms = ctor.GetParameters()
                        .Select(prm => ServiceProviderServiceExtensions.GetRequiredService(_provider, prm.ParameterType))
                        .ToArray();

                    var cls = (ITextPacketEvent) Activator.CreateInstance(e, prms);
                    cls.Data = (Dictionary<string, string>) data;
                    cls.Run(sender);
                }
            }
            
            // TODO: Add more Event Types
            
        }
    }
}