// Author: Tyler Franklin
// New Code added for Fork: BlazorDB-issue-11
using System.Collections.Generic;

namespace BlazorDB
{
    /// <summary>
    /// Upgrades a store schema
    /// </summary>
    public class StoreSchemaUpgrade
    {
        // Store to upgrade
        public string Name { get; set; }
        // Upgrade action to perform
        public string UpgradeAction { get; set; }
        // Object to contain parameters needed to complete the action
        public object[] UpgradeActionParameterList { get; set; }
        // List of column to perform action AND/OR delete
        public List<string> ColumnsToPerformActionOn { get; set; }
        // Column to receive data after action is performed
        public List<string> ColumnsToReceiveDataFromAction { get; set; }
    }
}
