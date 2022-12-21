window.blazorDB = {
    databases: [],
    getDbNames: function () {
        return new Promise(resolve => Dexie.getDatabaseNames().then(resolve));
    },
    getDbStoreDynamic: function (dbName) {
        return new Promise((resolve, reject) => {
            Dexie.getDatabaseNames().then(dbNames => {
                if (dbNames.find(n => n === dbName) === undefined) {
                    console.error("Database not found");
                    reject("No database");
                } else {
                    var db = new Dexie(dbName);

                    db.open().then(db => {
                        const dbStore = {
                            name: db.name,
                            version: db.verno,
                            storeSchemas: db.tables.map(table => {
                                return {
                                    name: table.name,
                                    primaryKey: table.schema.primKey.name,
                                    primaryKeyAuto: table.schema.primKey.auto,
                                    uniqueIndexes: table.schema.indexes
                                        .filter(index => index.unique)
                                        .map(index => index.name),
                                    indexes: table.schema.indexes
                                        .filter(index => !index.unique)
                                        .map(index => index.name)
                                };
                            }),
                            dynamic: true
                        };
                        // Problem: when we create a dynamic db, we make it version 1 in createDb() below.
                        // However, if we close/exit our session without adding any store schemas,
                        // the next time we call db.open() on this dynamic db, the reported verno is 0,
                        // rather than 1. Yet browser devtools DOES report the correct version.
                        // Also, calling indexedDB directly will also report correct version.
                        // (note, for unrelated reasons, Dexie version is the indexedDB version / 10,
                        // see https://github.com/dfahlander/Dexie.js/issues/59)
                        // To fix: when we come across verno = 0, change the version number in dbStore to 1.
                        // dbStore needs correct version so we can increment it when a store schema is added.
                        // This is not ideal; need to understand why Dexie reports verno 0 instead of 1.
                        if (db.verno === 0) {
                            dbStore.version = 1;
                            indexedDB.databases().then(databases => {
                                console.warn(`Dexie reports version of ${db.name} is ${db.verno}, but indexedDB reports version is ${databases.find(db1 => db1.name === db.name).version / 10}. BlazorDB will use indexedDB version number.`);
                            });
                        }
                        window.blazorDB.databases.push({
                            name: dbName,
                            db: db
                        });
                        resolve(dbStore);
                    })
                }
            })
        });
    },
    addSchema: function (dotnetReference, transaction, dbStore, newStoreSchema) {
        if (!dbStore.dynamic || window.blazorDB.databases.find(d => d.name === dbStore.name) === undefined) {
            console.error(`Blazor.IndexedDB.Framework - Dynamic database ${dbStore.name} not found`);
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, `Dynamic database ${dbStore.name} could not be found`);
            return;
        }
        var db = window.blazorDB.databases.find(d => d.name === dbStore.name).db
        if (db.tables.map(table => table.name).find(name => name === newStoreSchema.name) !== undefined) {
            console.error(`Blazor.IndexedDB.Framework - schema for store ${newStoreSchema.name} already exists`);
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Schema already exists');
            return;
        }
        db.close();

        dbStore.storeSchemas.push(newStoreSchema);
        this.createDb(dotnetReference, transaction, dbStore);
    },
    createDb: function (dotnetReference, transaction, dbStore) {
        if(!dbStore.dynamic && window.blazorDB.databases.find(d => d.name === dbStore.name) !== undefined)
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
        if (dbStore.dynamic) {
            dbStore.version = dbStore.version + 1;
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
        db.open().then(db => {
            if (dbStore.dynamic)
                window.blazorDB.databases.find(d => d.name === dbStore.name).db = db;
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Database opened');
        }).catch(e => {
            console.error(e);
            dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Database could not be opened');
        });
    },
    deleteDb: function(dotnetReference, transaction, dbName) {
        window.blazorDB.getDb(dbName).then(db => {
            var index = window.blazorDB.databases.findIndex(d => d.name == dbName);
            window.blazorDB.databases.splice(index, 1);
            db.delete().then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Database deleted');
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Database could not be deleted');
            });
        });
    },
    addItem: function(dotnetReference, transaction, item) {
        window.blazorDB.getTable(item.dbName, item.storeName).then(table => {
            table.add(item.record).then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item added');
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item could not be added');
            });
        });
    },
    bulkAddItem: function(dotnetReference, transaction, dbName, storeName, items) {
        window.blazorDB.getTable(dbName, storeName).then(table => {
            table.bulkAdd(items).then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item(s) bulk added');
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item(s) could not be bulk added');
            });
        });
    },
    putItem: function(dotnetReference, transaction, item) {
        window.blazorDB.getTable(item.dbName, item.storeName).then(table => {
            table.put(item.record).then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item put successful');
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item put failed');
            });
        });
    },
    updateItem: function(dotnetReference, transaction, item) {
        window.blazorDB.getTable(item.dbName, item.storeName).then(table => {
            table.update(item.key, item.record).then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item updated');
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item could not be updated');
            });
        });
    },
    deleteItem: function(dotnetReference, transaction, item) {
        window.blazorDB.getTable(item.dbName, item.storeName).then(table => {
            table.delete(item.key).then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Item deleted');
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Item could not be deleted');
            });
        });
    },
    clear: function(dotnetReference, transaction, dbName, storeName) {
        window.blazorDB.getTable(dbName, storeName).then(table => {
            table.clear().then(_ => {
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Table cleared');
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Table could not be cleared');
            });
        });
    },
    findItem: function(dotnetReference, transaction, item) {
        var promise = new Promise((resolve, reject) => {
            window.blazorDB.getTable(item.dbName, item.storeName).then(table => {
                table.get(item.key).then(i => {
                    dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'Found item');
                    resolve(i);
                }).catch(e => {
                    console.error(e);
                    dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'Could not find item');
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
                    dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'toArray succeeded');
                    resolve(items);
                }).catch(e => {
                    console.error(e);
                    dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'toArray failed');
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
                    dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, false, 'where succeeded');
                    resolve(items);
                })
            }).catch(e => {
                console.error(e);
                dotnetReference.invokeMethodAsync('BlazorDBCallback', transaction, true, 'where failed');
                reject(e);
            });
        });
    }
}