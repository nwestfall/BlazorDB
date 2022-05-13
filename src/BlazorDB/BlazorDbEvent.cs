using System;
using System.Text.Json;

namespace BlazorDB
{
    public class BlazorDbEvent
    {
        public Guid Transaction { get; set; }
        public bool Failed { get; set; }
        public string Message { get; set; }
        public JsonDocument DbResult { get; set; }
    }
}