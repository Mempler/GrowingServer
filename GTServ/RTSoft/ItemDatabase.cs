using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Generated.ItemsDb.Enums;
using Generated.ItemsDb.Item;

namespace GTServ.RTSoft
{
    public class ItemDatabase
    {
        public static ItemDatabase Instance { get; private set; }
        public const ushort ItemDatabaseVersion = 11;
        public Dictionary<ItemId, Item> Items;

        private byte[] _packedData;
        private uint _hash;
        
        public uint Hash
        {
            get
            {
                if (_packedData == null)
                    Pack();
                
                if (_hash == 0)
                    _hash = (uint) GtHasher.Hash(_packedData);

                return _hash;
            }
        }

        public Item Find(int id) => Items.TryGetValue((ItemId) id, out var item) ? item : null;
        public Item Find(ItemId id) =>  Items.TryGetValue(id, out var item) ? item : null;

        public ItemDatabase()
        {
            Instance = this;
            
            var generatedItemAssembly = typeof(Item).Assembly;
            var customItemsAssembly = typeof(ItemDatabase).Assembly;


            var generatedItemTypes = generatedItemAssembly.GetTypes()
                .Where(x => x.Namespace != null && x.Namespace.StartsWith("Generated.ItemsDb.Items")
                            && x.IsSubclassOf(typeof(Item))).ToList();
            
            var customItemsTypes = customItemsAssembly.GetTypes()
                .Where(x => x.Namespace != null && x.Namespace.StartsWith("GTServ.Items")
                                                && x.IsSubclassOf(typeof(Item))).ToList();

            Console.WriteLine($"Loading {generatedItemTypes.Count + customItemsTypes.Count} Items");

            var items = generatedItemTypes.Select(itemType => (Item) Activator.CreateInstance(itemType)).ToList();

            items.Sort((x,y) => ((int)x.ItemId).CompareTo((int) y.ItemId));
            
#if DEBUG
            items.RemoveRange(2000, items.Count - 2000);
#endif

            items.AddRange(customItemsTypes.Select(itemType => (Item) Activator.CreateInstance(itemType)));
            
            items.Sort((x,y) => ((int)x.ItemId).CompareTo((int) y.ItemId));

#if DEBUG
            Console.WriteLine($"Items have been limited to {items.Count} during Debugging!");
#endif

            Items = new Dictionary<ItemId, Item>();
            foreach (var item in items)
                Items.Add(item.ItemId, item);
        }

        public byte[] Pack()
        {
            if (_packedData != null)
                return _packedData;

            Console.WriteLine($"Packing {Items.Count} Items");
            
            using var ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes(ItemDatabaseVersion));
            ms.Write(BitConverter.GetBytes(Items.Count));
            
            foreach (var item in Items)
            {
                ms.Write(item.Value.Pack());
            }

            _packedData = ms.ToArray();
            
            File.WriteAllBytes("items.dat", _packedData);
            return _packedData;
        }
    }
}