using BlazorDB.Example;
using BlazorDB;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazorDB(options =>
{
    options.Name = "Test";
    options.Version = 2;
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
    options.StoreSchemaUpgrades = new List<StoreSchemaUpgrade>()
    {
        new StoreSchemaUpgrade()
        {
            Name = "Person",
            UpgradeAction = IndexedDbUpgradeFunctions.SPLIT_COLUMN,
            UpgradeActionParameterList = new object[] { 'N' },
            ColumnsToPerformActionOn = new List<string> { "name" },
            ColumnsToReceiveDataFromAction = new List<string> { "first", "last" }
        }
    };
});
builder.Services.AddBlazorDB(options =>
{
    options.Name = "Test2";
    options.Version = 11;
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
