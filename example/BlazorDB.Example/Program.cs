using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorDB.Example
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddBlazorDB(options =>
            {
                options.Name = "Test";
                options.Version = 1;
                options.StoreSchemas = new List<StoreSchema>()
                {
                    new StoreSchema()
                    {
                        Name = "Person",
                        PrimaryKey = "id",
                        PrimaryKeyAuto = true,
                        UniqueIndexes = new List<string> { "guid" },
                        Indexes = new List<string> { "name" }
                    }
                };
            });
            builder.Services.AddBlazorDB(options =>
            {
                options.Name = "Test2";
                options.Version = 1;
                options.StoreSchemas = new List<StoreSchema>()
                {
                    new StoreSchema()
                    {
                        Name = "Item",
                        PrimaryKey = "id",
                        PrimaryKeyAuto = true,
                        UniqueIndexes = new List<string> { "guid" },
                        Indexes = new List<string> { "name" }
                    }
                };
            });

            await builder.Build().RunAsync();
        }
    }
}
