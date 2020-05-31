using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorDB
{
    /// <summary>
    /// Provides functionality for accessing IndexedDB from Blazor application
    /// </summary>
    public class IndexedDBManager
    {
        private readonly DbStore _dbStore;
        private readonly IJSRuntime _jsRuntime;
        private const string InteropPrefix = "window.blazorDB";
        private DotNetObjectReference<IndexedDBManager> _objReference;
        private IDictionary<Guid, WeakReference<Action<BlazorDBEvent>>> _transactions = new Dictionary<Guid, WeakReference<Action<BlazorDBEvent>>>();
        private IDictionary<Guid, TaskCompletionSource<BlazorDBEvent>> _taskTransactions = new Dictionary<Guid, TaskCompletionSource<BlazorDBEvent>>();

        /// <summary>
        /// A notification event that is raised when an action is completed
        /// </summary>
        public event EventHandler<BlazorDBEvent> ActionCompleted;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="dbStore"></param>
        /// <param name="jsRuntime"></param>
        public IndexedDBManager(DbStore dbStore, IJSRuntime jsRuntime)
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
        public async Task<Guid> OpenDb(Action<BlazorDBEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            await CallJavascriptVoid(IndexedDBFunctions.CREATE_DB, trans, _dbStore);
            return trans;
        }

        /// <summary>
        /// Deletes the database corresponding to the dbName passed in
        /// </summary>
        /// <param name="dbName">The name of database to delete</param>
        /// <returns></returns>
        public async Task DeleteDb(string dbName, Action<BlazorDBEvent> action = null)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentException("dbName cannot be null or empty", nameof(dbName));
            }
            var trans = GenerateTransaction(action);
            await CallJavascriptVoid(IndexedDBFunctions.DELETE_DB, trans, dbName);
        }

        /// <summary>
        /// Deletes the database corresponding to the dbName passed in
        /// Waits for response
        /// </summary>
        /// <param name="dbName">The name of database to delete</param>
        /// <returns></returns>
        public async Task<BlazorDBEvent> DeleteDbAsync(string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentException("dbName cannot be null or empty", nameof(dbName));
            }
            var trans = GenerateTransaction();
            await CallJavascriptVoid(IndexedDBFunctions.DELETE_DB, trans.trans, dbName);
            return await trans.task;
        }

        /// <summary>
        /// Adds a new record/object to the specified store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordToAdd">An instance of StoreRecord that provides the store name and the data to add</param>
        /// <returns></returns>
        public async Task AddRecord<T>(StoreRecord<T> recordToAdd, Action<BlazorDBEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            try
            {
                recordToAdd.DbName = DbName;
                await CallJavascriptVoid(IndexedDBFunctions.ADD_ITEM, trans, recordToAdd);
            }
            catch (JSException e)
            {
                RaiseEvent(trans, true, e.Message);
            }
        }

        /// <summary>
        /// Adds a new record/object to the specified store
        /// Waits for response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordToAdd">An instance of StoreRecord that provides the store name and the data to add</param>
        /// <returns></returns>
        public async Task<BlazorDBEvent> AddRecordAsync<T>(StoreRecord<T> recordToAdd)
        {
            var trans = GenerateTransaction();
            try
            {
                recordToAdd.DbName = DbName;
                await CallJavascriptVoid(IndexedDBFunctions.ADD_ITEM, trans.trans, recordToAdd);
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
        public async Task UpdateRecord<T>(UpdateRecord<T> recordToUpdate, Action<BlazorDBEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            try
            {
                recordToUpdate.DbName = DbName;
                await CallJavascriptVoid(IndexedDBFunctions.UPDATE_ITEM, trans, recordToUpdate);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans, true, jse.Message);
            }
        }

        /// <summary>
        /// Updates and existing record
        /// Waits for response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordToUpdate">An instance of UpdateRecord with the store name and the record to update</param>
        /// <returns></returns>
        public async Task<BlazorDBEvent> UpdateRecordAsync<T>(UpdateRecord<T> recordToUpdate)
        {
            var trans = GenerateTransaction();
            try
            {
                recordToUpdate.DbName = DbName;
                await CallJavascriptVoid(IndexedDBFunctions.UPDATE_ITEM, trans.trans, recordToUpdate);
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
        /// <param name="storeName">The name of the  store to retrieve the record from</param>
        /// <param name="id">the id of the record</param>
        /// <returns></returns>
        public async Task<TResult> GetRecordByIdAsync<TInput, TResult>(string storeName, TInput key)
        {
            var trans = GenerateTransaction(null);

            var data = new { DbName = DbName, StoreName = storeName, Key = key };
            try
            {
                return await CallJavascript<TResult>(IndexedDBFunctions.FIND_ITEM, trans, data);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans, true, jse.Message);
            }

            return default(TResult);
        }
        
        /// <summary>
        /// Deletes a record from the store based on the id
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="storeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DeleteRecord<TInput>(string storeName, TInput key, Action<BlazorDBEvent> action = null)
        {
            var trans = GenerateTransaction(action);
            var data = new { DbName = DbName, StoreName = storeName, Key = key };
            try
            {
                await CallJavascriptVoid(IndexedDBFunctions.DELETE_ITEM, trans, data);
            }
            catch (JSException jse)
            {
                RaiseEvent(trans, true, jse.Message);
            }
        }

        /// <summary>
        /// Deletes a record from the store based on the id
        /// Wait for response
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="storeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<BlazorDBEvent> DeleteRecordAsync<TInput>(string storeName, TInput key)
        {
            var trans = GenerateTransaction();
            var data = new { DbName = DbName, StoreName = storeName, Key = key };
            try
            {
                await CallJavascriptVoid(IndexedDBFunctions.DELETE_ITEM, trans.trans, data);
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
                WeakReference<Action<BlazorDBEvent>> r = null;
                _transactions.TryGetValue(transaction, out r);
                TaskCompletionSource<BlazorDBEvent> t = null;
                _taskTransactions.TryGetValue(transaction, out t);
                if(r != null && r.TryGetTarget(out Action<BlazorDBEvent> action))
                {
                    action?.Invoke(new BlazorDBEvent()
                    {
                        Transaction = transaction,
                        Message = message,
                        Failed = failed
                    });
                    _transactions.Remove(transaction);
                }
                else if(t != null)
                {
                    t.TrySetResult(new BlazorDBEvent()
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

        private async Task<TResult> CallJavascript<TResult>(string functionName, Guid transaction, params object[] args)
        {
            var newArgs = GetNewArgs(transaction, args);
            return await _jsRuntime.InvokeAsync<TResult>($"{InteropPrefix}.{functionName}", newArgs);
        }
        private async Task CallJavascriptVoid(string functionName, Guid transaction, params object[] args)
        {
            var newArgs = GetNewArgs(transaction, args);
            await _jsRuntime.InvokeVoidAsync($"{InteropPrefix}.{functionName}", newArgs);
        }

        private object[] GetNewArgs(Guid transaction, params object[] args)
        {
            var newArgs = new object[args.Length + 2];
            newArgs[0] = _objReference;
            newArgs[1] = transaction;
            for(var i = 0; i < args.Length; i++)
                newArgs[i + 2] = args[i];
            return newArgs;
        }

        private (Guid trans, Task<BlazorDBEvent> task) GenerateTransaction()
        {
            bool generated = false;
            var transaction = Guid.Empty;
            TaskCompletionSource<BlazorDBEvent> tcs = new TaskCompletionSource<BlazorDBEvent>();
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

        private Guid GenerateTransaction(Action<BlazorDBEvent> action)
        {
            bool generated = false;
            Guid transaction = Guid.Empty;
            do
            {
                transaction = Guid.NewGuid();
                if(!_transactions.ContainsKey(transaction))
                {
                    generated = true;
                    _transactions.Add(transaction, new WeakReference<Action<BlazorDBEvent>>(action));
                }
            } while(!generated);
            return transaction;
        }

        private void RaiseEvent(Guid transaction, bool failed, string message)
            => ActionCompleted?.Invoke(this, new BlazorDBEvent { Transaction = transaction, Failed = failed, Message = message });
    }
}