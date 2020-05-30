window.blazorDB = {
    databases: [],
    createDb: function(dotnetReference, transaction, dbStore) {
        if(window.blazorDB.databases.find(d => d.name == dbStore.name) !== undefined)
            console.warn("Blazor.IndexedDB.Framework - Database already exists");

        var db = new Dexie(dbStore.name);

        var stores = {};
        for(var i = 0; i < dbStore.storeSchemas.length; i++) {
            // build string
            var schema = dbStore.storeSchemas[i];
            var def = "";
            if(schema.primaryKeyAuto)
                def = def + "++";
            if(schema.primaryKey !== null && schema.primaryKey !== "")
                def = def + schema.primaryKey;
            if(schema.uniqueIndexes !== undefined) {
                for(var j = 0; j < schema.uniqueIndexes.length; j++) {
                    def = def + ",";
                    var u = schema.uniqueIndexes[j];
                    def = def + u;
                }
            }
            
            stores[schema.name] = def;
        }
        db.version(dbStore.version).stores(stores);
        if(window.blazorDB.databases.find(d => d.name == dbStore.name) !== undefined) {
            window.blazorDB.databases.find(d => d.name == dbStore.name).db = db;
        } else {
            window.blazorDB.databases.push({
                name: dbStore.name,
                db: db
            });
        }
        db.open().then(_ => {
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Database opened');
        }).catch(e => {
            console.error(e);
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Database could not be opened');
        });
    },
    deleteDb: function(dotnetReference, transaction, dbName) {
        var db = window.blazorDB.getDb(dbName);
        var index = window.blazorDB.databases.findIndex(d => d.name == dbName);
        window.blazorDB.databases.splice(index, 1);
        db.delete().then(_ => {
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Database deleted');
        }).catch(e => {
            console.error(e);
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Database could not be deleted');
        })
    },
    addItem: function(dotnetReference, transaction, item) {
        var table = window.blazorDB.getTable(item.dbName, item.storeName);
        table.add(item.record).then(_ => {
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item added');
        }).catch(e => {
            console.error(e);
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item could not be added');
        })
    },
    updateItem: function(dotnetReference, transaction, item) {
        var table = window.blazorDB.getTable(item.dbName, item.storeName);
        table.update(item.key, item.record).then(_ => {
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item updated');
        }).catch(e => {
            console.error(e);
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item could not be updated');
        });
    },
    deleteItem: function(dotnetReference, transaction, item) {
        var table = window.blazorDB.getTable(item.dbName, item.storeName);
        table.delete(item.key).then(_ => {
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item deleted');
        }).catch(e => {
            console.error(e);
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item could not be deleted');
        })
    },
    findItem: function(dotnetReference, transaction, item) {
        var table = window.blazorDB.getTable(item.dbName, item.storeName);
        var promise = new Promise((resolve, reject) => {
            table.get(item.key).then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Found item');
                resolve(_);
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Could not find item');
                reject(e);
            });
        });
        return promise;
    },
    getDb: function(dbName) {
        if(window.blazorDB.databases.find(d => d.name == dbName) === undefined)
            console.warn("Blazor.IndexedDB.Framework - Database doesn't exist");
        
        var db = window.blazorDB.databases.find(d => d.name == dbName).db;
        return db;
    },
    getTable: function(dbName, storeName) {
        var db = window.blazorDB.getDb(dbName);
        var table = db[storeName];
        return table;
    }
}