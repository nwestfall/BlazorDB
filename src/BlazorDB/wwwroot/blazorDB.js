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
                    var u = "&" + schema.uniqueIndexes[j];
                    def = def + u;
                }
            }
            if (schema.indexes !== undefined) {
                for (var j = 0; j < schema.indexes.length; j++) {
                    def = def + ",";
                    var u = schema.indexes[j];
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
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Database opened', null);
        }).catch(e => {
            console.error(e);
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Database could not be opened', null);
        });
    },
    deleteDb: function(dotnetReference, transaction, dbName) {
        window.blazorDB.getDb(dbName).then(db => {
            var index = window.blazorDB.databases.findIndex(d => d.name == dbName);
            window.blazorDB.databases.splice(index, 1);
            db.delete().then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Database deleted', null);
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Database could not be deleted', null);
            });
        });
    },
    addItem: function(dotnetReference, transaction, item) {
        window.blazorDB.getTable(item.dbName, item.storeName).then(table => {
            table.add(item.record).then(result => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item added', result);
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item could not be added', null);
            });
        });
    },
    bulkAddItem: function(dotnetReference, transaction, dbName, storeName, items) {
        window.blazorDB.getTable(dbName, storeName).then(table => {
            table.bulkAdd(items).then(result => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item(s) bulk added', result);
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item(s) could not be bulk added', null);
            });
        });
    },
    putItem: function(dotnetReference, transaction, item) {
        window.blazorDB.getTable(item.dbName, item.storeName).then(table => {
            table.put(item.record).then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item put successful', null);
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item put failed', null);
            });
        });
    },
    updateItem: function(dotnetReference, transaction, item) {
        window.blazorDB.getTable(item.dbName, item.storeName).then(table => {
            table.update(item.key, item.record).then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item updated', null);
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item could not be updated', null);
            });
        });
    },
    deleteItem: function(dotnetReference, transaction, item) {
        window.blazorDB.getTable(item.dbName, item.storeName).then(table => {
            table.delete(item.key).then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item deleted', null);
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item could not be deleted', null);
            });
        });
    },
    clear: function(dotnetReference, transaction, dbName, storeName) {
        window.blazorDB.getTable(dbName, storeName).then(table => {
            table.clear().then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Table cleared', null);
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Table could not be cleared', null);
            });
        });
    },
    findItem: function(dotnetReference, transaction, item) {
        var promise = new Promise((resolve, reject) => {
            window.blazorDB.getTable(item.dbName, item.storeName).then(table => {
                table.get(item.key).then(i => {
                    dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Found item', null);
                    resolve(i);
                }).catch(e => {
                    console.error(e);
                    dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Could not find item', null);
                    reject(e);
                });
            });
        });
        return promise;
    },
    toArray: function(dotnetReference, transaction, dbName, storeName) {
        return new Promise((resolve, reject) => {
            window.blazorDB.getTable(dbName, storeName).then(table => {
                table.toArray(items => {
                    dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'toArray succeeded', null);
                    resolve(items);
                }).catch(e => {
                    console.error(e);
                    dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'toArray failed', null);
                    reject(e);
                });
            });
        });
    },
    getDb: function(dbName) {
        return new Promise((resolve, reject) => {
            if(window.blazorDB.databases.find(d => d.name == dbName) === undefined) {
                console.warn("Blazor.IndexedDB.Framework - Database doesn't exist");
                var db1 = new Dexie(dbName);
                db1.open().then(function (db) {
                    if(window.blazorDB.databases.find(d => d.name == dbName) !== undefined) {
                        window.blazorDB.databases.find(d => d.name == dbName).db = db1;
                    } else {
                        window.blazorDB.databases.push({
                            name: dbName,
                            db: db1
                        });
                    }
                    resolve(db1);
                }).catch('NoSuchDatabaseError', function(e) {
                    // Database with that name did not exist
                    console.error ("Database not found");
                    reject("No database");
                });
            } else {
                var db = window.blazorDB.databases.find(d => d.name == dbName).db;
                resolve(db);
            }
        });
    },
    getTable: function(dbName, storeName) {
        return new Promise((resolve, reject) => {
            window.blazorDB.getDb(dbName).then(db => {
                var table = db.table(storeName);
                resolve(table);
            });
        });
    },
    createFilterObject: function (filters) {
        const jsonFilter = {};
        for (const filter in filters) {
            if (filters.hasOwnProperty(filter))
                jsonFilter[filters[filter].indexName] = filters[filter].filterValue;
        }
        return jsonFilter;
    }, 
    where: function(dotnetReference, transaction, dbName, storeName, filters) {
        const filterObject = this.createFilterObject(filters);
        return new Promise((resolve, reject) => {
            window.blazorDB.getTable(dbName, storeName).then(table => {
                table.where(filterObject).toArray(items => {
                    dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'where succeeded', null);
                    resolve(items);
                })
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'where failed', null);
                reject(e);
            });
        });
    }
}