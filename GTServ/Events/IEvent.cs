using System.Collections.Generic;

namespace GTServ.Events
{
    public interface IEvent<T>
    {
       T Data { get; set; }
        void Run(Player sender);
    }

    public interface ITextPacketEvent : IEvent<Dictionary<string, string>>
    {
    }
}