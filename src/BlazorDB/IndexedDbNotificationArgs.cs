using System;

namespace BlazorDB
{
    public class BlazorDBEvent
    {
        public Guid Transaction { get; set; }
        public bool Failed { get; set; }
        public string Message { get; set; }
    }
}