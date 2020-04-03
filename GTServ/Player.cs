using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using ENet.Managed;
using Generated.ItemsDb.Enums;
using Generated.ItemsDb.Item;
using GTServ.RTSoft;

namespace GTServ
{
    public class InventoryItem
    {
        public Item Item;
        public ushort Amount;
    }
    public class Inventory
    {
        public int GemCount;
        public int SkinColor;

        public uint InventorySize = 16;
        public List<InventoryItem> Items = new List<InventoryItem>();

        public bool AddItem(ItemId id, int amount)
        {
            var item = new InventoryItem
            {
                Item = ItemDatabase.Instance.Find(id),
                Amount = (ushort) amount
            };

            return AddItem(item);
        }

        public bool AddItem(InventoryItem item)
        {
            if (InventorySize <= Items.Count)
                return false;
            
            Items.Add(item);
            return true;
        }

        public byte[] Pack()
        {
            using var ms = new MemoryStream();
            using var bs = new BinaryWriter(ms);


            bs.Write((byte) 0); // Dunno
            bs.Write((uint) InventorySize); // Inv Size
            bs.Write((byte) Items.Count);
            foreach (var item in Items)
            {
                bs.Write((ushort) item.Item.ItemId);
                bs.Write((ushort) item.Amount);
            }

            return ms.ToArray();
        }
    }
    
    public class Player
    {
        public ENetPeer Peer { get; internal set; }
        public World ActiveWorld { get; set; } = null; // "Exit World"
        public ENetServer ActiveServer { get; set; } = null; // "Exit World"

        public int NetId = -1;
        public int UserId = 1;
        public string Country = "XX";

        public string DisplayName;
        public string Name;

        public string PlatformSuffix = "_pc"; // TODO: Replace this with GT flags.
        public int NameNr;
        
        public bool HasGrowId;
        public bool HasLoggedIn;

        public int SwitchToken; // Used to authentify the User.
        public bool IsServerSwitching = false;

        public Inventory Inventory = new Inventory();
        
        /* World Information */
        public Vector2 Position;
        
        public Player(ENetPeer peer)
        {
            Peer = peer;
            
            Peer.OnReceive += OnReceive;
            Peer.Data = this;

            Inventory.InventorySize = 16;
            Inventory.AddItem(ItemId.i18_Fist, 1);
            Inventory.AddItem(ItemId.i32_Wrench, 1);
        }
        
        #region TankPackets
        public void AcceptLogin(string userName, string password)
        {
            Call("OnSuperMainStartAcceptLogonHrdxs47254722215a",
                ItemDatabase.Instance.Hash,
                "ubistatic-a.akamaihd.net",
                "0098/CDNContent/cache/",
                "cc.cz.madkite.freedom org.aqua.gg idv.aqua.bulldog com.cih.gamecih2 com.cih.gamecih com.cih.game_cih cn.maocai.gamekiller com.gmd.speedtime org.dax.attack com.x0.strai.frep com.x0.strai.free org.cheatengine.cegui org.sbtools.gamehack com.skgames.traffikrider org.sbtoods.gamehaca com.skype.ralder org.cheatengine.cegui.xx.multi1458919170111 com.prohiro.macro me.autotouch.autotouch com.cygery.repetitouch.free com.cygery.repetitouch.pro com.proziro.zacro com.slash.gamebuster",
                "proto=42|choosemusic=audio/mp3/about_theme.mp3|active_holiday=0|");

            Name = userName;
            DisplayName = userName + "_" + NameNr;
            
            SendHasGrowId(userName, password);
            SendConsoleMessage($"Welcome back, {ColorTable.White}{DisplayName} ({Name}){ColorTable.Default}!");
        }
        
        public void SendConsoleMessage(string message, params object[] prms)
        {
            Call("OnConsoleMessage", string.Format(message, prms));
        }

        public void SendItemsDatabase()
        {
            var header = GetTankPacketHeader();
            var packet = new SItemDatabaseResponsePacket(header);
            
            SendPacket(packet);
        }

        public void SendHasGrowId(string username, string password)
        {
            Call("SetHasGrowID",  HasGrowId ? 1u : 0u, username, password);
        }

        public void SendToWorldMenu()
        {
            Call("OnRequestWorldSelectMenu",  "default|TEST\n"); // TODO: Add World Menu
        }

        public void SendWorld(World target)
        {
            var header = GetTankPacketHeader(true);
            SendInventory();
            
            var worldPacket = new SWorldPacket(target, header);
            SendPacket(worldPacket);
            
            SendInventory();
            target.SpawnLocalPlayer(this);
        }

        public void SendInventory()
        {
            var header = GetTankPacketHeader();
            var packet = new SInventoryPackt(Inventory, header);
            
            SendPacket(packet);
            
            SendClothing();
        }

        public void SendClothing()
        {
            var header = GetTankPacketHeader();
            
            /*
            Call("OnSetClothing",
                );
            */
            
        }

        public void SendMovement()
        {
            var header = GetTankPacketHeader();

            var packet = new SMovementPacket(header);
            
            SendPacket(packet);
        }
        #endregion

        #region Packet
        public TankPacketHeader GetTankPacketHeader(bool needsNetId = false)
        {
            return new TankPacketHeader
            {
                NetId = needsNetId ? NetId : -1,
                Position = new Vector2(),
                CharacterState = 8,
                PlantingTree = 0
            };
        }

        public void Call(string func, params object[] prms)
        {
            var cFunc = VariantFunction.Call(func, prms);
            SendFunction(cFunc);
        }
        
        public void SendFunction(VariantFunction func)
        {
            ISPacket pack = new SVariantPacket(func, GetTankPacketHeader());
            
            SendPacket(pack);
        }
        
        public void SendPacket(ISPacket packet)
        {
            if (packet == null)
                throw new NullReferenceException();
            
            var pd = packet.Pack();
            var packetData = new byte[pd.Length + 5];
            
            // Copy Packet Type into Packet
            Array.Copy(BitConverter.GetBytes(packet.PacketType), 0, packetData, 0, 4);
            Array.Copy(pd, 0, packetData, 4, pd.Length);

            packetData[4 + pd.Length] = 0; // C String 0

            Peer.Send(packetData, 0, ENetPacketFlags.Reliable);
            Peer.Host.Flush();
        }
        #endregion
        
        public void OnConnect()
        {
            // Lets Request login Information.
            ISPacket pack = new SLoginInformationRequestPacket();
            SendPacket(pack);
        }

        public void OnDisconnect()
        {
            ActiveWorld?.ConnectedPlayers.Remove(this);
        }
        
        private void OnReceive(object sender, ENetPacket e)
        {
            //SendMovement();
            var peer = sender as ENetPeer;
            var packet = e.GetPayloadCopy();
           
            int packetType = packet[0];
            switch (packetType)
            {
                case 3:
                case 2:
                {
                    var packetData = CTextPacket.Unpack(packet);
                    if (!HasLoggedIn)
                    {
                        var requestedName = packetData["requestedName"];
                        var f = packetData["f"];
                        var protocol = packetData["protocol"];
                        var gameVersion = packetData["game_version"];
                        var fz = packetData["fz"];
                        var lMode = packetData["lmode"];
                        var cBits = packetData["cbits"];
                        var playerAge = packetData["player_age"];
                        var gdpr = packetData["GDPR"];
                        var hash2 = packetData["hash2"];
                        var meta = packetData["meta"];
                        var fHash = packetData["fhash"];
                        var rid = packetData["rid"];
                        var platformId = packetData["platformID"];
                        var deviceVersion = packetData["deviceVersion"];
                        var country = packetData["country"];
                        var hash = packetData["hash"];
                        var mac = packetData["mac"];
                        var wk = packetData["wk"];
                        var zf = packetData["zf"];

                        var rnd = new Random(wk.GetHashCode());

                        Country = country;

                        NameNr = rnd.Next(999);
                        HasLoggedIn = true;

                        AcceptLogin(requestedName, string.Empty);
                        return;
                    }
                    
                    EventManager.Instance.RunEvent(EventType.TextPacket,
                        packetData["action"],
                        this, packetData);
                } break;
                case 4:
                {
                    var (header, tankPacketData) = CTankPacket.Unpack(packet);

                    switch (header.TankPacketType)
                    {
                        case TankPacketType.VariantList:
                            break;
                        default:
                            Console.WriteLine($"Unknown TankPacket Type! {(int) header.TankPacketType}\n" +
                                              $"\tData: {BitConverter.ToString(tankPacketData).Replace("-", " ")}");
                            break;
                    }
                } break;
                default:
                    Console.WriteLine($"Unknown PacketType! {packetType}\n" +
                                      $"\tData: {BitConverter.ToString(packet, 4).Replace("-", " ")}");
                    break;
            }
        }
    }
}