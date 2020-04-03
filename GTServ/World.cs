using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Generated.ItemsDb.Enums;
using Generated.ItemsDb.Item;
using GTServ.RTSoft;

namespace GTServ
{
    public enum WorldType
    {
        Normal,
        Empty
    }
    
    public enum WorldBlockStatus
    {
    
    }
    
    public class WorldBlock
    {
        public Item Foreground;
        public Item Background;

        public WorldBlockStatus Status;

        private Dictionary<string, object> MetaData;

        public void SetMeta<T>(string k, T x)
        {
	        MetaData[k] = x;
        }

        public bool TryGetMeta<T>(string k, out T x)
        {
	        var z = MetaData.TryGetValue(k, out var xy);
	        x = (T) xy;
	        return z;
        }
    }
    
    public class WorldItem
    {
	    public Item Item;
	    public ushort Amount;
	    public Vector2 Position;

	    public ushort PickupId;
    }

    public enum WorldStatus
    {
        None,
        Jammed,
        Disabled
    }
    
    
    public class World
    {
        public const ulong WorldVersion = 0x01;
        public string Name;
        public int Width, Height;

        public Vector2 Spawn;

        public WorldStatus Status;
        
        public ENetServer CurrentServer { get; internal set; }
        
        public List<Player> ConnectedPlayers { get; } = new List<Player>();
        public List<WorldBlock> Blocks { get; } = new List<WorldBlock>();
        public List<WorldItem> DroppedItems { get; } = new List<WorldItem>();

        protected World(string name, int width, int height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public void SpawnLocalPlayer(Player player)
        {
	        player.Position = Spawn * 32;
	        player.NetId = ConnectedPlayers.Count;

	        var spawnAvatarStr = 
							   $"spawn|avatar\n" +
	                           $"netID|{player.NetId}\n" +
	                           $"userID|{player.UserId}\n" +
	                           $"colrect|0|0|20|30\n" +
	                           $"posXY|{player.Position.X}|{player.Position.Y}\n" +
	                           $"name|{player.DisplayName}\n" +
	                           $"country|{player.Country}\n" +
	                           $"invis|0\n" +
	                           $"mstate|0\n" +
	                           $"smstate|0\n" +
	                           $"type|local\n";
	        
	        player.Call("OnSpawn", spawnAvatarStr);
        }

        public void Broadcast(VariantFunction packet)
        {
	        foreach (var ply in ConnectedPlayers)
		        ply.SendFunction(packet);
        }
        public void Broadcast(ISPacket packet)
        {
	        foreach (var ply in ConnectedPlayers)
		        ply.SendPacket(packet);
        }
        
        public static World Load(string name)
        {
            if (!Directory.Exists("worlds"))
                Directory.CreateDirectory("worlds");

            World world;
            if (!File.Exists("worlds/" + name.ToLower() + ".world"))
            {
	            world = Generate(name);
	            world.Save();
	            return world;
            }
            
            using var fs = File.OpenRead("worlds/" + name.ToLower() + ".world");
            using var br = new BinaryReader(fs);

            var worldVersion = br.ReadUInt64();
            if (br.ReadUInt64() != 0x444C524F57)
            {
                Console.WriteLine($"World {name} is corrupted! create new empty world");
                return Generate(name);
            }
            
            world = new World(string.Empty, 0, 0)
            {
                Name =  br.ReadString(),
                Width = br.ReadInt32(),
                Height = br.ReadInt32(),
                
                Spawn = new Vector2(br.ReadSingle(), br.ReadSingle()),
                
                Status = (WorldStatus) br.ReadInt32()
            };

            var blockCount = br.ReadInt32();
            for (var i = 0; i < blockCount; i++)
            {
                var wb = new WorldBlock
                {
                    Foreground = ItemDatabase.Instance.Find(br.ReadInt32()),
                    Background = ItemDatabase.Instance.Find(br.ReadInt32()),
                    Status = (WorldBlockStatus) br.ReadInt32()
                };
                world.Blocks.Add(wb);
            }

            return world;
        }
        
        public void Save()
        {
            if (!Directory.Exists("worlds"))
                Directory.CreateDirectory("worlds");

            using var fs = File.OpenWrite("worlds/" + Name.ToLower() + ".world");
            using var bs = new BinaryWriter(fs);

            bs.Write(WorldVersion);

            bs.Write(0x444C524F57); // Magic Value (WORLD)
            bs.Write(Name);
            bs.Write(Width);
            bs.Write(Height);
            
            bs.Write(Spawn.X);
            bs.Write(Spawn.Y);
            
            bs.Write((int) Status);
            
            bs.Write(Blocks.Count);
            foreach (var block in Blocks)
            {
	            bs.Write((int) block.Foreground.ItemId);
                bs.Write((int) block.Background.ItemId);
                bs.Write((int) block.Status);
            }
        }

        public static World Generate(string name, int width = 100, int height = 60, WorldType worldType = WorldType.Normal)
        {
            var world = new World(name, width, height);
            var random = new Random();
            
            var bedrockStart = height < 7 ?  1 : height - 6;
            var floorStart = bedrockStart - height / 2 < 1 ? 1 : bedrockStart - height / 2;
            var doorX = random.Next(width);
            var doorY = floorStart - 1;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
	                var worldBlock = new WorldBlock {Status = 0};

	                switch (worldType)
                    {
                        case WorldType.Normal:
                            if (y >= bedrockStart) {
                                worldBlock.Foreground = ItemDatabase.Instance.Find(ItemId.i8_Bedrock);
                                worldBlock.Background = ItemDatabase.Instance.Find(ItemId.i14_CaveBackground);
                            } else if (y >= floorStart + 1 && y >= bedrockStart - 4  && random.Next(3) == 0) {
                                worldBlock.Foreground = ItemDatabase.Instance.Find(ItemId.i4_Lava);
                                worldBlock.Background = ItemDatabase.Instance.Find(ItemId.i14_CaveBackground);
                            } else if (y >= floorStart + 1 && random.Next(30) == 0) {
                                worldBlock.Foreground = ItemDatabase.Instance.Find(ItemId.i10_Rock);
                                worldBlock.Background = ItemDatabase.Instance.Find(ItemId.i14_CaveBackground);
                            } else if (y >= floorStart) {
                                worldBlock.Foreground = ItemDatabase.Instance.Find(ItemId.i2_Dirt);
                                worldBlock.Background = ItemDatabase.Instance.Find(ItemId.i14_CaveBackground);
                            } else {
                                worldBlock.Foreground = ItemDatabase.Instance.Find(ItemId.i0_Blank);
                                worldBlock.Background = ItemDatabase.Instance.Find(ItemId.i0_Blank);
                            }
                            
                            break;
                        case WorldType.Empty:
                            if (y >= bedrockStart) {
                                worldBlock.Foreground = ItemDatabase.Instance.Find(ItemId.i8_Bedrock);
                                worldBlock.Background = ItemDatabase.Instance.Find(ItemId.i14_CaveBackground);
                            } else {
                                worldBlock.Foreground = ItemDatabase.Instance.Find(ItemId.i0_Blank);
                                worldBlock.Background = ItemDatabase.Instance.Find(ItemId.i0_Blank);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(worldType), worldType, null);
                    }
                    
                    world.Blocks.Add(worldBlock);
                }
            }

            world.Spawn = new Vector2(doorX, doorY);

            //world.Blocks[doorX + doorY * width].Foreground = ItemDatabase.Instance.Find(ItemId.i6_MainDoor);
            //world.Blocks[doorX + (doorY + 1) * width].Foreground = ItemDatabase.Instance.Find(ItemId.i8_Bedrock);
            return world;
        }

        public byte[] Pack()
        {
            using var ms = new MemoryStream();
            using var bs = new BinaryWriter(ms);
            
            // Unknown
            bs.Write((ushort) 9);
            bs.Write((uint) 0);

            bs.Write((ushort) Name.Length);
            bs.Write(Encoding.ASCII.GetBytes(Name));
            
            bs.Write((uint) Width);
            bs.Write((uint) Height);
            
            bs.Write((uint) Width * Height);
            foreach (var block in Blocks)
            {
	            bs.Write((ushort) block.Foreground.ItemId);
	            bs.Write((ushort) block.Background.ItemId);
	            bs.Write((ushort) 0); // Lock Area Location
	            bs.Write((byte) 0); // Item Settings
	            bs.Write((byte) 0); // OnTop Layer (Colours)

	            var item = block.Foreground;

	            switch (item.ActionType) {
					case 2: // Doors
					case 13: // Main Door
					case 26: // Portals
					{
						bs.Write((byte) 1); // Type
						var doorText = "TEST";
						if (block.Foreground.ItemId == ItemId.i6_MainDoor)
							doorText = "EXIT";
						
						bs.Write((ushort) doorText.Length);
						bs.Write(Encoding.ASCII.GetBytes(doorText));
						bs.Write((byte) 0);  // (closed 0x8, opened 0x0)
						break;
					}
					case 3: // Locks
					{
						bs.Write((byte) 3); // Type
						bs.Write((byte) 3); // normal lock(0x0), area lock (0x1), custom music blocks disabled(0x10), custom music blocks invisible (0x20), custom music blocks disabled and custom music blocks invisible (0x30)
						bs.Write((uint) 0); // world owners userid
						bs.Write((uint) 0); // array size
						//when first is negative value then it's custom BPM value
						//all next are added players growids
						break;
					}
					case 10: // Sign
					{
						bs.Write((byte) 2);
						var signText = "TEST";
						bs.Write((ushort) signText.Length);
						bs.Write(Encoding.ASCII.GetBytes(signText.ToArray()));
						bs.Write(-1); // Color ?
						break;
					}
					case 19: // Seeds
					{
						bs.Write((byte) 4); // type
						bs.Write((uint) 0);  // time passed
						bs.Write((byte) 2);  //items on tree
						break;
					}
					case 33: // Mail Box
					case 34: // Bulletin Board
					{
						bs.Write((byte) 2);
						var bulletinBoardText = "TEST";
						bs.Write((ushort) bulletinBoardText.Length);
						bs.Write(Encoding.ASCII.GetBytes(bulletinBoardText.ToArray()));
						bs.Write(-1); // Color ?
						break;
					}
					case 36: // Roshambo Block, Dice Block
					{
						bs.Write((byte) 8); // type
						bs.Write((byte) 4); // ?
						break;
					}
					case 46: // Heart Monitor
					{
						bs.Write((byte) 11); // type
						bs.Write((uint) 1); // userId
						
						var heartMonitorText = "TEST";
						bs.Write((ushort) heartMonitorText.Length);
						bs.Write(Encoding.ASCII.GetBytes(heartMonitorText.ToArray()));
						break;
					}
					case 61: // Display Block
					{
						bs.Write((byte) 23); // type
						bs.Write((uint) 2); // itemId
						break;
					}
					case 62: // Vending Machine
					{
						bs.Write((byte) 24); // type
						bs.Write((uint) 2); // itemId
						bs.Write(-100); //  some value
						break;
					}
					case 67: // Giving Tree
					{
						bs.Write((byte) 28); // type
						bs.Write((ushort) 10240);  // time?
						bs.Write(-100); // %?
						break;
					}
					case 74: // Country Flag
					{
						bs.Write((byte) 33); // type
						var countryFlag = "EE";
						bs.Write((ushort) countryFlag.Length);
						bs.Write(Encoding.ASCII.GetBytes(countryFlag.ToArray()));
						break;
					}
					case 81: // Weather Machine Background
					{
						bs.Write((byte) 40); // type
						bs.Write(14); // background itemId
						break;
					}
					case 92: // DNA Processor
					{
						bs.Write((byte) 51); // type
						break;
					}
					default:
						//bs.Write((byte) 0);
						break;
				}
            }


            bs.Write((uint) DroppedItems.Count); // dropped items count
            bs.Write((uint) (DroppedItems.LastOrDefault()?.Item.ItemId ?? ItemId.i0_Blank));
            foreach (var item in DroppedItems)
            {
	            bs.Write(item.PickupId);
	            bs.Write(item.Position.X);
	            bs.Write(item.Position.Y);
	            bs.Write(item.Amount);
	            bs.Write((int) item.Amount);
	            bs.Write((int) item.Item.ItemId);
            }
            
            /*
			bs.Write((ushort) 0); // base weather (LOWORD only)
			bs.Write((ushort) 0); // idk yet what does this do
			bs.Write((ushort) 0); // current weather (LOWORD only)
			bs.Write((ushort) 0); // idk yet what does this do
			bs.Write((int) 0); // idk yet what does this do
			*/
            
            return ms.ToArray();
        }
    }
}