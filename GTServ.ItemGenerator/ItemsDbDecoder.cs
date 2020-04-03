using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GTServ.ItemGenerator
{
    public class ItemsDbDecoder
    {
        public ushort DbVersion;

        public List<Dictionary<string, object>> Data
            = new List<Dictionary<string, object>>();
        
        public static string DecryptName(int itemId, byte[] data)
        {
            if (itemId < 0)
                return "";
            
            const string secret = "PBG892FXX982ABC*";

            var retStr = string.Empty;
            
            for (var i = 0; i < data.Length; i++)
            {
                retStr += (char) (data[i] ^ secret[(i + itemId) % secret.Length]);
            }

            return retStr;
        }

        public void Decode(XElement cls, string path)
        {
            using var db = File.OpenRead(path);
            using var br = new BinaryReader(db);

            DbVersion = br.ReadUInt16();
            var itemCount = br.ReadUInt32();
            
            for (var i = 0; i < itemCount; i++)
            {
                var currentItem = new Dictionary<string, object>();
                foreach (var elm in cls.Elements())
                {
                    var nm = elm.Attribute("name")?.Value ?? throw new NullReferenceException();
                    switch (elm.Attribute("type")?.Value)
                    {
                        case "System.Int32":
                            currentItem[nm] = br.ReadInt32();
                            break;
                        case "System.SByte":
                            currentItem[nm] = br.ReadSByte();
                            break;
                        case "System.Byte":
                            currentItem[nm] = br.ReadByte();
                            break;
                        case "System.String":
                            var sLen = br.ReadUInt16();
                            var b = new byte[sLen];
                            br.Read(b);
                            if (elm.Attribute("encrypted") != null)
                                currentItem[nm] = DecryptName((int) currentItem["ItemId"], b);
                            else
                                currentItem[nm] = Encoding.ASCII.GetString(b);
                            break;
                        case "System.UInt16":
                            currentItem[nm] = br.ReadUInt16();
                            break;
                        case "System.Drawing.Color":
                            currentItem[nm] = Color.FromArgb(br.ReadInt32());
                            break;
                        case "System.TimeSpan":
                            currentItem[nm] = TimeSpan.FromMilliseconds(br.ReadInt32());
                            break;
                        case "System.Byte[]":
                            var count = int.Parse(elm.Attribute("count")?.Value ?? throw new NullReferenceException());

                            if (currentItem.ContainsKey("ParticleInfoLength"))
                                count += (ushort) currentItem["ParticleInfoLength"];
                            
                            b = new byte[count];
                            
                            br.Read(b);
                            currentItem[nm] = b;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                if (Data.Any(x => (string) x["Name"] == (string) currentItem["Name"]))
                    continue;
                
                Data.Add(currentItem);
            }
        }
    }
}