namespace BlazorDB
{
    public class UpdateRecord<T> : StoreRecord<T>
    {
        public object Key { get; set; }
    }
}