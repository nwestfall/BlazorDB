Forked from [https://github.com/Reshiru/Blazor.IndexedDB.Framework](https://github.com/Reshiru/Blazor.IndexedDB.Framework) as inspiration.  A lot has changed since original fork and a lot is planned to get it closer.

# BlazorDB

An easy, fast way to use IndexedDB in a Blazor application.

### How to Use (WASM)
 - Install `dotnet add package BlazorIndexedDB`
 - Add `.js` files to `index.html`
    ```js
    <script src="_content/BlazorIndexedDB/dexie.min.js"></script>
    <script src="_content/BlazorIndexedDB/blazorDB.js"></script>
    ```
 - Add `@using BlazorDB` to `_Imports.razor`
 - Update `Program.cs` `ServiceCollection` with new databases (can add as many databases as you want)
    ```c#
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
                UniqueIndexes = new List<string> { "name" }
            }
        };
    });
    ```
 - Inject `IBlazorDbFactory` in your Razor pages
    ```c#
    @inject IBlazorDbFactory _dbFactory
    ```
 - Start using!
    ```c#
    var manager = await _dbFactory.GetDbManager(dbName)
    await manager.AddRecord(new StoreRecord<object>()
    {
        StoreName = storeName,
        Record = new { Name = "MyName", Age = 20 }
    });
    ```

### How it works
You defined your databases and store schemes in the `ServicesCollection`.  We add a helper called `AddBlazorDB` where you can build ` DbStore`.  From there, we ensure that an instance of `IBlazorDbFactory` is available in your service provider and will automatically add all `DbStores` and `IndexedDbManagers` based on what is defined with `AddBlazorDb`.  This allows you to have multiple databases if needed.

To access any database/store, all you need to do is inject the `IBlazorDbFactory` in your page and call `GetDbManager(string dbName)`.  We will pull your `IndexedDbManager` from the factory and make sure it's created and ready.

Most calls will either be true `Async` or let you pass an `Action` in.  If it lets you pass in an `Action`, it is optional.  This `Action` will get called when the method is complete.  This way, it isn't blocking on `WASMs` single thread.  All of these calls return a `Guid` which is the "transaction" identifier to IndexeDB (it's not a true database transaction, just the call between C# and javascript that gets tracked).

If the call is flagged as `Async`, we will wait for the JS callback that it is complete and then return the data.  The library takes care of this connection between javascript and C# for you.

### Available Calls
 - `Task<Guid> OpenDb(Action<BlazorDbEvent> action)` - Open the IndexedDb and make sure it is created, with an optional callback when complete
 - `Task<Guid> DeleteDb(string dbName, Action<BlazorDbEvent> action)` - Delete the database, with an optional callback when complete
 - `Task<BlazorDbEvent> DeleteDbAsync(string dbName)` - Delete the database and wait for it to complete
 - `Task<Guid> AddRecord<T>(StoreRecord<T> recordToAdd, Action<BlazorDbEvent> action)` - Add a record to the store, with an optional callback when complete
 - `Task<BlazorDbEvent> AddRecordAsync<T>(StoreRecord<T> recordToAdd)` - Add a record to the store and wait for it to complete
 - `Task<Guid> BulkAddRecord<T>(string storeName, IEnumerable<T> recordsToBulkAdd, Action<BlazorDbEvent> action)` - Adds records/objects to the specified store in bulk
 - `Task<BlazorDbEvent> BulkAddRecordAsync<T>(string storeName, IEnumerable<T> recordsToBulkAdd)` - Adds records/objects to the specified store in bulk and waits for it to complete
 - `Task<Guid> UpdateRecord<T>(UpdateRecord<T> recordToUpdate, Action<BlazorDbEvent> action)` - Update a record in the store, with an optional callback when c complete
 - `Task<BlazorDbEvent> UpdateRecordAsync(UpdateRecord<T> recordToUpdate)` - Update a record in the store and wait for it to complete
 - `Task<TResult> GetRecordByIdAsync<TInput, TResult>(string storeName, TInput key)` - Get a record by the 'id' and wait for it to return
 - `Task<Guid> DeleteRecord<TInput>(string storeName, TInput key, Action<BlazorDbEvent> action)` - Delete a record in the store by 'id', with an optional callback when complete
 - `Task<BlazorDbEvent> DeleteRecordAsync<TInput>(string storeName, TInput key)` - Delete a record in the store by 'id' and wait for it to complete
 - `Task<BlazorDbEvent> ClearTable<TInput>(string storeName, Action<BlazorDbEvent> action)` - Clears all data from a Table but keeps the table
 - `Task<BlazorDbEvent> ClearTableAsync<TInput>(string storeName)` - Clears all data from a Table but keeps the table and wait for it to complete

### More to Come
 - Real queries
 - Automated testing
 - Dynamically add/remove from `IBlazorDbFactory`
