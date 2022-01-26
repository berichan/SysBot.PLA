using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SysBot.Pokemon
{
    public class LegalItem
    {
        public string Hash { get; set; } = string.Empty;
        public int ID { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public List<string> KeyWords { get; set; } = new List<string>();

        public LegalItem(string hash, int id, string name)
        {
            Hash = hash;
            ID = id;
            Name = name;
        }

        public LegalItem() { }
    }

    public class LegalItemLoader
    {
        private static LegalItemLoader? instance;
        public static LegalItemLoader Instance
        {
            get
            {
                if (instance == null)
                    instance = new LegalItemLoader();
                return instance;
            }
        }

        public IReadOnlyList<LegalItem> Items;

        public LegalItemLoader()
        {
            var itemJson = ResourceLoader.GetEmbeddedResource("SysBot.Pokemon.Resources", "legalitems.json");
            var nItems = JsonSerializer.Deserialize<List<LegalItem>>(itemJson);
            if (nItems != null)
                Items = nItems;
            else
                throw new Exception("Unable to load resource: ItemList");
        }

        public int? GetItemIdFromString(string str)
        {
            var li = GetLegalItemFromString(str);
            if (li != null)
                return li.ID;
            return null;
        }

        public LegalItem? GetLegalItemFromString(string str)
        {
            return Items.FirstOrDefault(x => x.Hash == str);
        }
    }
}
