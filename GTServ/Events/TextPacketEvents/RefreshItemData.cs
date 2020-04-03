using System.Collections.Generic;

namespace GTServ.Events.TextPacketEvents
{
    [TextPacketEventAttribute(ActionType = "refresh_item_data")]
    public class RefreshItemData : ITextPacketEvent
    {
        public Dictionary<string, string> Data { get; set; }
        
        public void Run(Player sender)
        {
            sender.SendItemsDatabase();
        }
    }
}