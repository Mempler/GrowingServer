using System.Collections.Generic;
using GTServ.RTSoft;

namespace GTServ.Events.TextPacketEvents
{
    [TextPacketEvent(ActionType = "join_request")]
    public class JoinRequest : ITextPacketEvent
    {
        public Dictionary<string, string> Data { get; set; }
        
        public void Run(Player sender)
        {
            var worldName = Data["name"];
            var hasBeenInvited = Data["invitedWorld"];

            var world = WorldManager.Instance.Load(worldName);
            if (world.ConnectedPlayers.Contains(sender))
                world.ConnectedPlayers.Remove(sender);

            if (world.CurrentServer != sender.ActiveServer)
            {
                // TODO: Move to a Different Sub Server!
            }
            
            world.ConnectedPlayers.Add(sender);
            // TODO: Add join packet for other users!

            sender.ActiveWorld = world;
            
            sender.Position = world.Spawn;
            sender.SendWorld(world);
        }
    }
}