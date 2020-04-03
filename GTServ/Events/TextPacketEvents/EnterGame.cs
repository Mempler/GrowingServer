using System.Collections.Generic;

namespace GTServ.Events.TextPacketEvents
{  
    [TextPacketEventAttribute(ActionType = "enter_game")]
    public class EnterGame : ITextPacketEvent
    {
        public Dictionary<string, string> Data { get; set; }
        
        public void Run(Player sender)
        {
            sender.SendToWorldMenu();
        }
    }
}