namespace BlazorDB
{
    public class IndexFilterValue
    {
        public IndexFilterValue(string indexName, object filterValue)
        {
            IndexName = indexName;
            FilterValue = filterValue;
        }

        public string IndexName { get; set; }
        public object FilterValue { get; set; }
    }
}