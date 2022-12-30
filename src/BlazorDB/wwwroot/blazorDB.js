window.blazorDB = {
    databases: [],
    createDb: function(dotnetReference, transaction, dbStore) {
        if (window.blazorDB.databases.find(d => d.name == dbStore.name) !== undefined) {
            console.warn("Blazor.IndexedDB.Framework - Database already exists");
        }
            
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

        // New Code for Fork: BlazorDB-issue-11
        // True IF a StoreSchemaUpdate object has been passed via service builder in Program.cs
        if (dbStore.storeSchemaUpgrades !== null) {
            // See function signature for why this is not a promise.
            // Errors that occur in this function will be caught/logged to console when db.open() is called.
            this.upgradeDbSchema(db, stores, dbStore);
        }
        else {
            db.version(dbStore.version).stores(stores);
        }
        // End New Code for Fork: BlazorDB-issue-11

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
    },
    // Assumption. Only one schema update per version update
    // Will allow multiple updates after proof of concept
    // This function purposefully DOES NOT return a promise. This is because .upgrade() DOES NOT return a promise.
    upgradeDbSchema: function (db, stores, dbStore) {
            // Get information to build the update function
            var schemaUpdate = dbStore.storeSchemaUpgrades[0];
            //Assumption: This currently only works for 1 schema. TODO: ALLOW MULTIPLE SCHEMA UPDATES.
            switch (schemaUpdate.upgradeAction) {
                case 'split':
                    db.version(dbStore.version).stores(stores).upgrade(trans => {
                        // Get the table, cast to collection, call necessary function
                        return trans.table(schemaUpdate.name).toCollection().modify(row => {
                            // modify each row
                            row[schemaUpdate.columnsToReceiveDataFromAction[0]] = row[schemaUpdate.columnsToPerformActionOn[0]].split(schemaUpdate.upgradeActionParameterList[0])[0];
                            row[schemaUpdate.columnsToReceiveDataFromAction[1]] = row[schemaUpdate.columnsToPerformActionOn[0]].split(schemaUpdate.upgradeActionParameterList[0])[1];
                            delete row[schemaUpdate.columnsToPerformActionOn[0]];
                        });
                    });
                    break;
                case 'multiply':
                    // Intent take value of columnsToPerformActionOn, multiple by value in upgradeActionParameterList,
                    db.version(dbStore.version).stores(stores).upgrade(trans => {
                        // Get the table, cast to collection, call necessary function
                        return trans.table(schemaUpdate.name).toCollection().modify(row => {
                            // modify each row
                            for (var i = 0; i < schemaUpdate.upgradeActionParameterList.length; i++) {
                                row[schemaUpdate.columnsToReceiveDataFromAction[i]] = Math.round((row[schemaUpdate.columnsToPerformActionOn[i]] * schemaUpdate.upgradeActionParameterList[i]) * 100) / 100;
                            }
                        });
                    });
                    break;
                case 'multiply-delete':
                    // This deletes the column the value we are multiplying in in. 
                    db.version(dbStore.version).stores(stores).upgrade(trans => {
                        // Get the table, cast to collection, call necessary function
                        return trans.table(schemaUpdate.name).toCollection().modify(row => {
                            // modify each row
                            for (var i = 0; i < schemaUpdate.upgradeActionParameterList.length; i++) {
                                row[schemaUpdate.columnsToReceiveDataFromAction[i]] = Math.round((row[schemaUpdate.columnsToPerformActionOn[i]] * schemaUpdate.upgradeActionParameterList[i]) * 100) / 100;
                                delete row[schemaUpdate.columnsToPerformActionOn[i]];
                            }
                        });
                    });
                    break;
                case 'divide':
                    db.version(dbStore.version).stores(stores).upgrade(trans => {
                        // Get the table, cast to collection, call necessary function
                        return trans.table(schemaUpdate.name).toCollection().modify(row => {
                            // modify each row
                            for (var i = 0; i < schemaUpdate.upgradeActionParameterList.length; i++) {
                                row[schemaUpdate.columnsToReceiveDataFromAction[i]] = Math.round((row[schemaUpdate.columnsToPerformActionOn[i]] / schemaUpdate.upgradeActionParameterList[i]) * 100) / 100;
                            }
                        });
                    });
                    break;
                case 'divide-delete':
                    db.version(dbStore.version).stores(stores).upgrade(trans => {
                        // Get the table, cast to collection, call necessary function
                        return trans.table(schemaUpdate.name).toCollection().modify(row => {
                            // modify each row
                            for (var i = 0; i < schemaUpdate.upgradeActionParameterList.length; i++) {
                                row[schemaUpdate.columnsToReceiveDataFromAction[i]] = Math.round((row[schemaUpdate.columnsToPerformActionOn[i]] / schemaUpdate.upgradeActionParameterList[i]) * 100) / 100;
                                delete row[schemaUpdate.columnsToPerformActionOn[i]];
                            }
                        });
                    });
                    break;
                case 'add':
                    db.version(dbStore.version).stores(stores).upgrade(trans => {
                        // Get the table, cast to collection, call necessary function
                        return trans.table(schemaUpdate.name).toCollection().modify(row => {
                            // modify each row
                            for (var i = 0; i < schemaUpdate.upgradeActionParameterList.length; i++) {
                                row[schemaUpdate.columnsToReceiveDataFromAction[i]] = Math.round((row[schemaUpdate.columnsToPerformActionOn[i]] + schemaUpdate.upgradeActionParameterList[i]) * 100) / 100;
                                //delete row[schemaUpdate.columnsToPerformActionOn[i]];
                            }
                        });
                    });
                    break;
                case 'add-delete':
                    db.version(dbStore.version).stores(stores).upgrade(trans => {
                        // Get the table, cast to collection, call necessary function
                        return trans.table(schemaUpdate.name).toCollection().modify(row => {
                            // modify each row
                            for (var i = 0; i < schemaUpdate.upgradeActionParameterList.length; i++) {
                                row[schemaUpdate.columnsToReceiveDataFromAction[i]] = Math.round((row[schemaUpdate.columnsToPerformActionOn[i]] + schemaUpdate.upgradeActionParameterList[i]) * 100) / 100;
                                delete row[schemaUpdate.columnsToPerformActionOn[i]];
                            }
                        });
                    });
                    break;
                case 'subtract':
                    db.version(dbStore.version).stores(stores).upgrade(trans => {
                        // Get the table, cast to collection, call necessary function
                        return trans.table(schemaUpdate.name).toCollection().modify(row => {
                            // modify each row
                            for (var i = 0; i < schemaUpdate.upgradeActionParameterList.length; i++) {
                                row[schemaUpdate.columnsToReceiveDataFromAction[i]] = Math.round((row[schemaUpdate.columnsToPerformActionOn[i]] - schemaUpdate.upgradeActionParameterList[i]) * 100) / 100;
                                //delete row[schemaUpdate.columnsToPerformActionOn[i]];
                            }
                        });
                    });
                    break;
                case 'subtract-delete':
                    db.version(dbStore.version).stores(stores).upgrade(trans => {
                        // Get the table, cast to collection, call necessary function
                        return trans.table(schemaUpdate.name).toCollection().modify(row => {
                            // modify each row
                            for (var i = 0; i < schemaUpdate.upgradeActionParameterList.length; i++) {
                                row[schemaUpdate.columnsToReceiveDataFromAction[i]] = Math.round((row[schemaUpdate.columnsToPerformActionOn[i]] - schemaUpdate.upgradeActionParameterList[i]) * 100) / 100;
                                delete row[schemaUpdate.columnsToPerformActionOn[i]];
                            }
                        });
                    });
                    break;
                case 'delete-column':
                    db.version(dbStore.version).stores(stores).upgrade(trans => {
                        // Get the table, cast to collection, call necessary function
                        return trans.table(schemaUpdate.name).toCollection().modify(row => {
                            // modify each row
                            for (var i = 0; i < schemaUpdate.columnsToPerformActionOn.length; i++) {
                                //row[schemaUpdate.columnsToReceiveDataFromAction[i]] = Math.round((row[schemaUpdate.columnsToPerformActionOn[i]] - schemaUpdate.upgradeActionParameterList[i]) * 100) / 100;
                                delete row[schemaUpdate.columnsToPerformActionOn[i]];
                            }
                        });
                    });
                    break;
                default:
                    console.warn('default');
            }
    }
}