using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BlazorDB
{
    public class BlazorDbFactory : IBlazorDbFactory
    {
        readonly IJSRuntime _jsRuntime;
        readonly IServiceProvider _serviceProvider;
        readonly IDictionary<string, IndexedDbManager> _dbs = new Dictionary<string, IndexedDbManager>();
        const string InteropPrefix = "window.blazorDB";

        public BlazorDbFactory(IServiceProvider serviceProvider, IJSRuntime jSRuntime)
        {
            _serviceProvider = serviceProvider;
            _jsRuntime = jSRuntime;
        }

        public async Task<IndexedDbManager> GetDbManager(string dbName)
        {
            if(!_dbs.Any())
                await BuildFromServices();
            if(_dbs.ContainsKey(dbName))
                return _dbs[dbName];
            if ((await GetDbNames()).Contains(dbName))
                await BuildExistingDynamic(dbName);
            else
                await BuildNewDynamic(dbName);

            return _dbs[dbName];
        }

        public Task<IndexedDbManager> GetDbManager(DbStore dbStore)
            => GetDbManager(dbStore.Name);

        async Task BuildFromServices()
        {
            var dbStores = _serviceProvider.GetServices<DbStore>();
            if(dbStores != null)
            {
                foreach(var dbStore in dbStores)
                {
                    Console.WriteLine($"{dbStore.Name}{dbStore.Version}{dbStore.StoreSchemas.Count}");
                    var db = new IndexedDbManager(dbStore, _jsRuntime, _dbs);
                    await db.OpenDb();
                    _dbs.Add(dbStore.Name, db);
                }
            }
        }

        async Task BuildExistingDynamic(string dbName)
        {
            var dbStore = await GetDbStoreDynamic(dbName);
            var db = new IndexedDbManager(dbStore, _jsRuntime, _dbs);
            _dbs.Add(dbStore.Name, db);
        }

        async Task BuildNewDynamic(string dbName)
        {
            var newDbStore = new DbStore() { Name = dbName, StoreSchemas = new List<StoreSchema>(), Version = 0, Dynamic = true };
            var newDb = new IndexedDbManager(newDbStore, _jsRuntime, _dbs);
            await newDb.OpenDb();
            newDbStore.Version = 1;
            _dbs.Add(newDbStore.Name, newDb);
        }

        public async Task<string[]> GetDbNames()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string[]>($"{InteropPrefix}.{IndexedDbFunctions.GET_DB_NAMES}");
            }
            catch (JSException jse)
            {
                throw new Exception($"Could not get database names. JavaScript exception: {jse.Message}");
            }
        }

        async Task<DbStore> GetDbStoreDynamic(string dbName)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<DbStore>($"{InteropPrefix}.{IndexedDbFunctions.GET_DB_STORE_DYNAMIC}", new object[] { dbName });
            }
            catch (JSException jse)
            {
                throw new Exception($"Could not get dynamic database store configuration. JavaScript exception: {jse.Message}");
            }
        }
    }
}