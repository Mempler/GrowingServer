using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace GTServ.RTSoft
{
    public enum TankPacketType : uint
    {
        Movement     = 0x00,
        VariantList  = 0x01,
        TileChange   = 0x03,
        World        = 0x04,
        SendTile     = 0x05,
        UpdateTile   = 0x06,
        Inventory    = 0x09,
        ItemDatabase = 0x10,
    }
    
    public struct TankPacketHeader
    {
        public TankPacketType TankPacketType;
        public bool OverrideTankPacketType;
        public int NetId;
        public uint CharacterState;
        public uint PlantingTree;
        public Vector2 Position;
        public Vector2 Speed;
        public Vector2 PunchDirection;

        public bool SkipData;
        public uint PacketLength;

        public readonly byte[] Pack()
        {
            using var ms = new MemoryStream();
            
            var pt = BitConverter.GetBytes((uint) TankPacketType);
            var nid = BitConverter.GetBytes(NetId);
            var p1 = BitConverter.GetBytes(0);
            var cs = BitConverter.GetBytes(CharacterState);
            var p2 = BitConverter.GetBytes(0);
            var pnt = BitConverter.GetBytes(PlantingTree);
            var posX = BitConverter.GetBytes(Position.X);
            var posY = BitConverter.GetBytes(Position.Y);
            var spdX = BitConverter.GetBytes(Speed.X);
            var spdY = BitConverter.GetBytes(Speed.Y);
            var pd3 = BitConverter.GetBytes(0);
            var pdX = BitConverter.GetBytes((int) Math.Round(PunchDirection.X));
            var pdY = BitConverter.GetBytes((int) Math.Round(PunchDirection.Y));
            var pl = BitConverter.GetBytes(PacketLength);
            
            ms.Write(pt);
            ms.Write(nid);
            ms.Write(p1);
            ms.Write(cs);
            ms.Write(p2);
            ms.Write(pnt);
            ms.Write(posX);
            ms.Write(posY);
            ms.Write(spdX);
            ms.Write(spdY);
            ms.Write(pd3);
            ms.Write(pdX);
            ms.Write(pdY);
            if (!SkipData)
                ms.Write(pl);

            return ms.ToArray();
        }

        public int Unpack(byte[] data, int index)
        {
            var currentIndex = index;
            
            TankPacketType = (TankPacketType) BitConverter.ToUInt32(data, currentIndex); currentIndex += 4;
            NetId = BitConverter.ToInt32(data, currentIndex); currentIndex += 4;
            currentIndex += 4;
            CharacterState = BitConverter.ToUInt32(data, currentIndex); currentIndex += 4;
            currentIndex += 4;
            PlantingTree = BitConverter.ToUInt32(data, currentIndex); currentIndex += 4;
            Position.X = BitConverter.ToSingle(data, currentIndex); currentIndex += 4;
            Position.Y = BitConverter.ToSingle(data, currentIndex); currentIndex += 4;
            Speed.X = BitConverter.ToSingle(data, currentIndex); currentIndex += 4;
            Speed.Y = BitConverter.ToSingle(data, currentIndex); currentIndex += 4;
            currentIndex += 4;
            PunchDirection.X = BitConverter.ToInt32(data, currentIndex); currentIndex += 4;
            PunchDirection.Y = BitConverter.ToInt32(data, currentIndex); currentIndex += 4;
            PacketLength = BitConverter.ToUInt32(data, currentIndex); currentIndex += 4;
            
            return currentIndex;
        }
    }

    public interface IPacketType
    {
        int PacketType { get; }
    }
    
    #region ServerPackets
    public interface ISPacket : IPacketType
    {
        byte[] Pack();
    }
    
    public class STankPacket : ISPacket
    {
        public int PacketType => 0x04;
        public virtual TankPacketType TankPacketType => TankPacketType.Movement;
        
        protected TankPacketHeader Header;
        
        public byte[] Data;
        public int DataLength;

        public STankPacket(TankPacketHeader header)
        {
            Header = header;
        }

        public virtual byte[] Pack()
        {
            using var ms = new MemoryStream();

            Header.PacketLength = (uint) DataLength;
            Header.TankPacketType = Header.OverrideTankPacketType ? Header.TankPacketType : TankPacketType;

            if (Data == null)
                Header.SkipData = true;
            
            ms.Write(Header.Pack());
            if (Data != null)
                ms.Write(Data);

            return ms.ToArray();
        }
    }

    #region TankPackets
    public class SVariantPacket : STankPacket
    {
        public override TankPacketType TankPacketType => TankPacketType.VariantList;
        
        private readonly VariantFunction _function;
        
        public SVariantPacket(VariantFunction function, TankPacketHeader header) : base(header)
        {
            _function = function;
            Header.CharacterState = 8;
        }

        public override byte[] Pack()
        {
            Data = _function.Pack();
            DataLength = _function.Indices;

            return base.Pack();
        }
    }

    public class SItemDatabaseResponsePacket : STankPacket
    {
        public override TankPacketType TankPacketType => TankPacketType.ItemDatabase;

        public SItemDatabaseResponsePacket(TankPacketHeader header) : base(header)
        {
        }

        public override byte[] Pack()
        {
            var db = ItemDatabase.Instance.Pack();
            Data = db;
            DataLength = db.Length;

            Header.CharacterState = 8;

            return base.Pack();
        }
    }

    public class SWorldPacket : STankPacket
    {
        private readonly World _target;
        public override TankPacketType TankPacketType => TankPacketType.World;

        public SWorldPacket(World target, TankPacketHeader header) : base(header)
        {
            _target = target;
        }

        public override byte[] Pack()
        {
            var worldData = _target.Pack();
            
            Data = worldData;
            DataLength = worldData.Length;

            return base.Pack();
        }
    }

    public class SInventoryPackt : STankPacket
    {
        private readonly Inventory _inventory;
        public override TankPacketType TankPacketType => TankPacketType.Inventory;

        public SInventoryPackt(Inventory inventory, TankPacketHeader header) : base(header)
        {
            _inventory = inventory;
        }

        public override byte[] Pack()
        {
            var pack = _inventory.Pack();

            Data = pack;
            DataLength = pack.Length;
            
            return base.Pack();
        }
    }

    public class SMovementPacket : STankPacket
    {
        public override TankPacketType TankPacketType => TankPacketType.Movement;

        public SMovementPacket(TankPacketHeader header) : base(header)
        {
        }
    }
    #endregion

    public class STextPacket : ISPacket
    {
        private readonly string _s;
        public int PacketType => 0x03;

        public STextPacket(string s)
        {
            _s = s;
        }
        
        public byte[] Pack()
        {
            return Encoding.ASCII.GetBytes(_s);
        }
    }
    
    public class SLoginInformationRequestPacket : ISPacket
    {
        public int PacketType => 0x01;
        
        public byte[] Pack()
        {
            return new byte[0];
        }
    }

    #endregion
    
    #region ClientPackets
    public class CTankPacket
    {
        public static (TankPacketHeader header, byte[] tankPacketData) Unpack(byte[] packet)
        {
            var header = new TankPacketHeader();
            var readed = header.Unpack(packet, 4);

            var tankPacketData = new byte[header.PacketLength];
            
            Array.Copy(packet, readed, tankPacketData, 0, header.PacketLength);
            
            return (header, tankPacketData);
        }
    }

    public class CTextPacket
    {
        public static Dictionary<string, string> Unpack(byte[] packet)
        {
            var d = Encoding.ASCII.GetString(packet, 4, packet.Length - 4);

            var r = new Dictionary<string, string>();
            foreach (var d1 in d.Split('\n'))
            {
                if (d1.Length == 1)
                    continue;
                
                var c = d1.Split('|');
                r[c[0]] = c[1];
            }

            return r;
        } 
    }
    #endregion
}