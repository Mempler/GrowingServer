using System.Linq;

namespace GTServ.RTSoft
{
    public static class GtHasher
    {
        public static ulong Hash(byte[] d)
        {
            if (d.Length == 0)
                return 0;

            uint hash = 0x55555555;
            
            foreach (var t in d)
            {
                hash = (hash >> 27) + (hash << 5) + t;
            }

            return hash;
        }
    }
}