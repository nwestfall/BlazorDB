using System.Collections.Generic;

namespace BlazorDB
{
    public class DbStore
    {
        public string Name { get; set; }
        public int Version { get; set; }
        public List<StoreSchema> StoreSchemas { get; set; }
        public bool Dynamic { get; set; } = false;
    }
}