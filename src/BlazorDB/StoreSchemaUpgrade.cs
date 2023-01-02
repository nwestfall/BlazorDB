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
        /// <summary>
        /// Store to upgrade
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Upgrade action to perform
        /// </summary>
        public string UpgradeAction { get; set; }

        /// <summary>
        /// Object to contain parameters needed to complete the action
        /// </summary>
        public object[] UpgradeActionParameterList { get; set; }

        /// <summary>
        /// List of column to perform action AND/OR delete
        /// </summary>
        public List<string> ColumnsToPerformActionOn { get; set; }

        /// <summary>
        /// Column to receive data after action is performed
        /// </summary>
        public List<string> ColumnsToReceiveDataFromAction { get; set; }
    }
}
