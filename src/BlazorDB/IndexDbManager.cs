using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorDB
{
    /// <summary>
    /// Provides functionality for accessing IndexedDB from Blazor application
    /// </summary>
    public class IndexedDbManager
    {
        readonly DbStore _dbStore;
        readonly IJSRuntime _jsRuntime;
        const string InteropPrefix = "window.blazorDB";
        DotNetObjectReference<IndexedDbManager> _objReference;
        IDictionary<Guid, WeakReference<Action<BlazorDbEvent>>> _transactions = new Dictionary<Guid, WeakReference<Action<BlazorDbEvent>>>();
        IDictionary<Guid, TaskCompletionSource<BlazorDbEvent>> _taskTransactions = new Dictionary<Guid, TaskCompletionSource<BlazorDbEvent>>();

        /// <summary>
        /// A notification event that is raised when an action is completed
        /// </summary>
        public event EventHandler<BlazorDbEvent> ActionCompleted;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="dbStore"></param>
        /// <param name="jsRuntime"></param>
        internal IndexedDbManager(DbStore dbStore, IJSRuntime jsRuntime)
        {
            _objReference = DotNetObjectReference.Create(this);
            _dbStore = dbStore;
            _jsRuntime = jsRuntime;
        }

        public List<StoreSchema> Stores => _dbStore.StoreSchemas;
        public int CurrentVersion => _dbStore.Version;
        public string DbName => _dbStore.Name;
        
        /// <summary>
        /// Opens the IndexedDB defined in the DbStore. Under the covers will create the database if it does not exist
        /// and create the stores defined in DbStore.
        /// </summary>
        /// <returns></returns>
        public async Task<Guid> OpenDb(Action<BlazorDbEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            await CallJavascriptVoid(IndexedDbFunctions.CREATE_DB, trans, _dbStore);
            return trans;
        }

        /// <summary>
        /// Deletes the database corresponding to the dbName passed in
        /// </summary>
        /// <param name="dbName">The name of database to delete</param>
        /// <returns></returns>
        public async Task<Guid> DeleteDb(string dbName, Action<BlazorDbEvent> action = null)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentException("dbName cannot be null or empty", nameof(dbName));
            }
            var trans = GenerateTransaction(action);
            await CallJavascriptVoid(IndexedDbFunctions.DELETE_DB, trans, dbName);
            return trans;
        }

        /// <summary>
        /// Deletes the database corresponding to the dbName passed in
        /// Waits for response
        /// </summary>
        /// <param name="dbName">The name of database to delete</param>
        /// <returns></returns>
        public async Task<BlazorDbEvent> DeleteDbAsync(string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentException("dbName cannot be null or empty", nameof(dbName));
            }
            var trans = GenerateTransaction();
            await CallJavascriptVoid(IndexedDbFunctions.DELETE_DB, trans.trans, dbName);
            return await trans.task;
        }

        /// <summary>
        /// Adds a new record/object to the specified store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordToAdd">An instance of StoreRecord that provides the store name and the data to add</param>
        /// <returns></returns>
        public async Task<Guid> AddRecord<T>(StoreRecord<T> recordToAdd, Action<BlazorDbEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            try
            {
                recordToAdd.DbName = DbName;
                await CallJavascriptVoid(IndexedDbFunctions.ADD_ITEM, trans, recordToAdd);
            }
            catch (JSException e)
            {
                RaiseEvent(trans, true, e.Message);
            }
            return trans;
        }

        /// <summary>
        /// Adds a new record/object to the specified store
        /// Waits for response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordToAdd">An instance of StoreRecord that provides the store name and the data to add</param>
        /// <returns></returns>
        public async Task<BlazorDbEvent> AddRecordAsync<T>(StoreRecord<T> recordToAdd)
        {
            var trans = GenerateTransaction();
            try
            {
                recordToAdd.DbName = DbName;
                await CallJavascriptVoid(IndexedDbFunctions.ADD_ITEM, trans.trans, recordToAdd);
            }
            catch (JSException e)
            {
                RaiseEvent(trans.trans, true, e.Message);
            }
            return await trans.task;
        }

        /// <summary>
        /// Adds records/objects to the specified store in bulk
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordsToBulkAdd">The data to add</param>
        /// <returns></returns>
        public async Task<Guid> BulkAddRecord<T>(string storeName, IEnumerable<T> recordsToBulkAdd, Action<BlazorDbEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            try
            {
                await CallJavascriptVoid(IndexedDbFunctions.BULKADD_ITEM, trans, DbName, storeName, recordsToBulkAdd);
            }
            catch (JSException e)
            {
                RaiseEvent(trans, true, e.Message);
            }
            return trans;
        }

        /// <summary>
        /// Adds records/objects to the specified store in bulk
        /// Waits for response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordsToBulkAdd">An instance of StoreRecord that provides the store name and the data to add</param>
        /// <returns></returns>
        public async Task<BlazorDbEvent> BulkAddRecordAsync<T>(string storeName, IEnumerable<T> recordsToBulkAdd)
        {
            var trans = GenerateTransaction();
            try
            {
                await CallJavascriptVoid(IndexedDbFunctions.BULKADD_ITEM, trans.trans, DbName, storeName, recordsToBulkAdd);
            }
            catch (JSException e)
            {
                RaiseEvent(trans.trans, true, e.Message);
            }
            return await trans.task;
        }

        /// <summary>
        /// Puts a new record/object to the specified store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordToPut">An instance of StoreRecord that provides the store name and the data to put</param>
        /// <returns></returns>
        public async Task<Guid> PutRecord<T>(StoreRecord<T> recordToPut, Action<BlazorDbEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            try
            {
                recordToPut.DbName = DbName;
                await CallJavascriptVoid(IndexedDbFunctions.PUT_ITEM, trans, recordToPut);
            }
            catch (JSException e)
            {
                RaiseEvent(trans, true, e.Message);
            }
            return trans;
        }

        /// <summary>
        /// Puts a new record/object to the specified store
        /// Waits for response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordToPut">An instance of StoreRecord that provides the store name and the data to put</param>
        /// <returns></returns>
        public async Task<BlazorDbEvent> PutRecordAsync<T>(StoreRecord<T> recordToPut)
        {
            var trans = GenerateTransaction();
            try
            {
                recordToPut.DbName = DbName;
                await CallJavascriptVoid(IndexedDbFunctions.PUT_ITEM, trans.trans, recordToPut);
            }
            catch (JSException e)
            {
                RaiseEvent(trans.trans, true, e.Message);
            }
            return await trans.task;
        }

        /// <summary>
        /// Updates and existing record
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordToUpdate">An instance of UpdateRecord with the store name and the record to update</param>
        /// <returns></returns>
        public async Task<Guid> UpdateRecord<T>(UpdateRecord<T> recordToUpdate, Action<BlazorDbEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            try
            {
                recordToUpdate.DbName = DbName;
                await CallJavascriptVoid(IndexedDbFunctions.UPDATE_ITEM, trans, recordToUpdate);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans, true, jse.Message);
            }
            return trans;
        }

        /// <summary>
        /// Updates and existing record
        /// Waits for response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordToUpdate">An instance of UpdateRecord with the store name and the record to update</param>
        /// <returns></returns>
        public async Task<BlazorDbEvent> UpdateRecordAsync<T>(UpdateRecord<T> recordToUpdate)
        {
            var trans = GenerateTransaction();
            try
            {
                recordToUpdate.DbName = DbName;
                await CallJavascriptVoid(IndexedDbFunctions.UPDATE_ITEM, trans.trans, recordToUpdate);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans.trans, true, jse.Message);
            }
            return await trans.task;
        }

        /// <summary>
        /// Retrieve a record by id
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="storeName">The name of the store to retrieve the record from</param>
        /// <param name="id">the id of the record</param>
        /// <returns></returns>
        public async Task<TResult> GetRecordByIdAsync<TInput, TResult>(string storeName, TInput key)
        {
            var trans = GenerateTransaction(null);

            var data = new { DbName = DbName, StoreName = storeName, Key = key };
            try
            {
                return await CallJavascript<TResult>(IndexedDbFunctions.FIND_ITEM, trans, data);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans, true, jse.Message);
            }

            return default(TResult);
        }

        /// <summary>
        /// Filter a store on an indexed value 
        /// </summary>
        /// <param name="storeName">The name of the store to retrieve the records from</param>
        /// <param name="indexName">index field name to filter on</param>
        /// <param name="filterValue">filter's value</param>
        /// <returns></returns>
        public async Task<IList<TRecord>> Where<TRecord>(string storeName, string indexName, object filterValue)
        {
            var filters = new List<IndexFilterValue>() { new IndexFilterValue(indexName, filterValue) };
            return await Where<TRecord>(storeName, filters);
        }

        /// <summary>
        /// Filter a store on indexed values 
        /// </summary>
        /// <param name="storeName">The name of the store to retrieve the records from</param>
        /// <param name="filters">A collection of index names and filters conditions</param>
        /// <returns></returns>
        public async Task<IList<TRecord>> Where<TRecord>(string storeName, IEnumerable<IndexFilterValue> filters)
        {
            var trans = GenerateTransaction(null);

            try
            {

                return await CallJavascript<IList<TRecord>>(IndexedDbFunctions.WHERE,  trans, DbName, storeName, filters);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans, true, jse.Message);
            }

            return default;
        }
        
        /// <summary>
        /// Retrieve all the records in a store
        /// </summary>
        /// <typeparam name="TRecord"></typeparam>
        /// <param name="storeName">The name of the store to retrieve the records from</param>
        /// <returns></returns>
        public async Task<IList<TRecord>> ToArray<TRecord>(string storeName)
        {
            var trans = GenerateTransaction(null);

            try
            {
                return await CallJavascript<IList<TRecord>>(IndexedDbFunctions.TOARRAY, trans, DbName, storeName);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans, true, jse.Message);
            }

            return default;
        }
        
        /// <summary>
        /// Deletes a record from the store based on the id
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="storeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Guid> DeleteRecord<TInput>(string storeName, TInput key, Action<BlazorDbEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            var data = new { DbName = DbName, StoreName = storeName, Key = key };
            try
            {
                await CallJavascriptVoid(IndexedDbFunctions.DELETE_ITEM, trans, data);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans, true, jse.Message);
            }
            return trans;
        }

        /// <summary>
        /// Deletes a record from the store based on the id
        /// Wait for response
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="storeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<BlazorDbEvent> DeleteRecordAsync<TInput>(string storeName, TInput key)
        {
            var trans = GenerateTransaction();
            var data = new { DbName = DbName, StoreName = storeName, Key = key };
            try
            {
                await CallJavascriptVoid(IndexedDbFunctions.DELETE_ITEM, trans.trans, data);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans.trans, true, jse.Message);
            }
            return await trans.task;
        }

        /// <summary>
        /// Clears all data from a Table but keeps the table
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<Guid> ClearTable(string storeName, Action<BlazorDbEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            try
            {
                await CallJavascriptVoid(IndexedDbFunctions.CLEAR_TABLE, trans, DbName, storeName);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans, true, jse.Message);
            }
            return trans;
        }

        /// <summary>
        /// Clears all data from a Table but keeps the table
        /// Wait for response
        /// </summary>
        /// <param name="storeName"></param>
        /// <returns></returns>
        public async Task<BlazorDbEvent> ClearTableAsync(string storeName)
        {
            var trans = GenerateTransaction();
            try
            {
                await CallJavascriptVoid(IndexedDbFunctions.CLEAR_TABLE, trans.trans, DbName, storeName);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans.trans, true, jse.Message);
            }
            return await trans.task;
        }

        [JSInvokable("BlazorDBCallback")]
        public void CalledFromJS(Guid transaction, bool failed, string message)
        {
            if(transaction != Guid.Empty)
            {
                WeakReference<Action<BlazorDbEvent>> r = null;
                _transactions.TryGetValue(transaction, out r);
                TaskCompletionSource<BlazorDbEvent> t = null;
                _taskTransactions.TryGetValue(transaction, out t);
                if(r != null && r.TryGetTarget(out Action<BlazorDbEvent> action))
                {
                    action?.Invoke(new BlazorDbEvent()
                    {
                        Transaction = transaction,
                        Message = message,
                        Failed = failed
                    });
                    _transactions.Remove(transaction);
                }
                else if(t != null)
                {
                    t.TrySetResult(new BlazorDbEvent()
                    {
                        Transaction = transaction,
                        Message = message,
                        Failed = failed
                    });
                    _taskTransactions.Remove(transaction);
                }
                else
                    RaiseEvent(transaction, failed, message);
            }
        }

        async Task<TResult> CallJavascript<TResult>(string functionName, Guid transaction, params object[] args)
        {
            var newArgs = GetNewArgs(transaction, args);
            return await _jsRuntime.InvokeAsync<TResult>($"{InteropPrefix}.{functionName}", newArgs);
        }
        async Task CallJavascriptVoid(string functionName, Guid transaction, params object[] args)
        {
            var newArgs = GetNewArgs(transaction, args);
            await _jsRuntime.InvokeVoidAsync($"{InteropPrefix}.{functionName}", newArgs);
        }

        object[] GetNewArgs(Guid transaction, params object[] args)
        {
            var newArgs = new object[args.Length + 2];
            newArgs[0] = _objReference;
            newArgs[1] = transaction;
            for(var i = 0; i < args.Length; i++)
                newArgs[i + 2] = args[i];
            return newArgs;
        }

        (Guid trans, Task<BlazorDbEvent> task) GenerateTransaction()
        {
            bool generated = false;
            var transaction = Guid.Empty;
            TaskCompletionSource<BlazorDbEvent> tcs = new TaskCompletionSource<BlazorDbEvent>();
            do
            {
                transaction = Guid.NewGuid();
                if(!_taskTransactions.ContainsKey(transaction))
                {
                    generated = true;
                    _taskTransactions.Add(transaction, tcs);
                }
            } while(!generated);
            return (transaction, tcs.Task);
        }

        Guid GenerateTransaction(Action<BlazorDbEvent> action)
        {
            bool generated = false;
            Guid transaction = Guid.Empty;
            do
            {
                transaction = Guid.NewGuid();
                if(!_transactions.ContainsKey(transaction))
                {
                    generated = true;
                    _transactions.Add(transaction, new WeakReference<Action<BlazorDbEvent>>(action));
                }
            } while(!generated);
            return transaction;
        }

        void RaiseEvent(Guid transaction, bool failed, string message)
            => ActionCompleted?.Invoke(this, new BlazorDbEvent { Transaction = transaction, Failed = failed, Message = message });
    }
}
