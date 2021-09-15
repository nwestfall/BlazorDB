using System.Collections.Generic;

namespace BlazorDB
{
    public class StoreSchema
    {
        public string Name { get; set; }
        public string PrimaryKey { get; set; }
        public bool PrimaryKeyAuto { get; set; }
        public List<string> UniqueIndexes { get; set; } = new List<string>();
        public List<string> Indexes { get; set; } = new List<string>();
    }
}