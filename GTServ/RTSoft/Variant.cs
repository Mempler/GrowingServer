using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace GTServ.RTSoft
{
    public enum VariantType
    {
        None = 0,
        Float = 1,
        Str = 2,
        Vec2 = 3,
        Vec3 = 4,
        UInt = 5,
        Int = 9,
    }

    public class VariantFunction
    {
        public Variant Name;
        public List<Variant> Variants;

        public int Indices => 1 + Variants.Count;

        public byte[] Pack()
        {
            var index = 0;
            using var s = new MemoryStream();
            
            s.WriteByte(0);
            
            s.WriteByte((byte) index++);
            s.Write(Name.Pack());
            
            foreach (var var in Variants)
            {
                s.WriteByte((byte) index++);
                s.Write(var.Pack());
            }

            var arr = s.ToArray();

            arr[0] = (byte) index;
            
            return arr;
        }

        public VariantFunction(string name)
        {
            Name = new Variant();
            Name.Set(name);
            
            Variants = new List<Variant>();
        }
        
        public static VariantFunction Call(string name, params object[] prms)
        {
            var varFunc = new VariantFunction(name);

            foreach (var prm in prms)
            {
                var var = new Variant();
                var.Set(prm);
                
                varFunc.Variants.Add(var);
            }

            return varFunc;
        }
    }

    public class Variant
    {
        public VariantType Type;

        public object Obj;

        public T Get<T>() => (T) Obj;
        
        public void Set<T>(T t)
        {
            var variantType = t.GetType();

            if (variantType == typeof(sbyte) ||
                variantType == typeof(short) ||
                variantType == typeof(int) ||
                variantType == typeof(long))
            {
                Type = VariantType.Int;
            }
            else if (variantType == typeof(byte) ||
                     variantType == typeof(ushort) ||
                     variantType == typeof(uint) ||
                     variantType == typeof(ulong))
            {
                Type = VariantType.UInt;
            }

            else if (variantType == typeof(float) ||
                     variantType == typeof(double))
            {
                Type = VariantType.Float;
            }


            else if (variantType == typeof(Vector2))
            {
                Type = VariantType.Vec2;
            }


            else if (variantType == typeof(Vector3))
            {
                Type = VariantType.Vec3;
            }

            else if (variantType == typeof(string))
            {
                Type = VariantType.Str;
            }

            else
                throw new ArgumentOutOfRangeException(nameof(t), $"T is not a valid VariantType! T was {t.GetType()}");

            Obj = t;
        }

        public byte[] Pack()
        {
           
            byte[] data;
            switch (Type)
            {
                case VariantType.Str:
                {
                    var d = Encoding.ASCII.GetBytes((string) Obj);
                    var l = BitConverter.GetBytes(((string) Obj).Length);
                    
                    data = new byte[1+4+d.Length];
                    data[0] = (byte) Type;
                    
                    Array.Copy(l, 0, data, 1, 4);
                    Array.Copy(d, 0, data, 5, d.Length);
                } break;
                case VariantType.Int:
                {
                    var d = BitConverter.GetBytes((int) Obj);
                    
                    data = new byte[5];
                    data[0] = (byte) Type;
                    Array.Copy(d, 0, data, 1, 4);
                } break;
                case VariantType.UInt:
                {
                    var d = BitConverter.GetBytes((uint) Obj);
                    
                    data = new byte[5];
                    data[0] = (byte) Type;
                    Array.Copy(d, 0, data, 1, 4);
                } break;
                case VariantType.Float:
                {
                    var d = BitConverter.GetBytes((float) Obj);
                    
                    data = new byte[5];
                    data[0] = (byte) Type;
                    Array.Copy(d, 0, data, 1, 4);
                } break;
                case VariantType.Vec2:
                {
                    var vec = (Vector2) Obj;
                    var d1 = BitConverter.GetBytes(vec.X);
                    var d2 = BitConverter.GetBytes(vec.Y);
                    
                    data = new byte[9];
                    data[0] = (byte) Type;
                    Array.Copy(d1, 0, data, 1, 4);
                    Array.Copy(d2, 0, data, 5, 4);
                } break;
                case VariantType.Vec3:
                {
                    var vec = (Vector3) Obj;
                    var d1 = BitConverter.GetBytes(vec.X);
                    var d2 = BitConverter.GetBytes(vec.Y);
                    var d3 = BitConverter.GetBytes(vec.Z);
                    
                    data = new byte[13];
                    data[0] = (byte) Type;
                    Array.Copy(d1, 0, data, 1, 4);
                    Array.Copy(d2, 0, data, 5, 4);
                    Array.Copy(d3, 0, data, 9, 4);
                } break;
                case VariantType.None:
                    data = new byte[1];
                    data[0] = (byte) Type;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return data;
        }
    }
}