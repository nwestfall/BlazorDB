// Author: Tyler Franklin
// New Code for Fork: BlazorDB-issue-11
namespace BlazorDB
{
    public struct IndexedDbUpgradeFunctions
    {
        /// <summary>
        /// Splits the data in one column into two new columns 
        /// 
        /// Must include two column names in the ColumnsToReceiveDataFromAction property
        /// </summary>
        public const string SPLIT_COLUMN = "split";

        /// <summary>
        /// Multiplies the value of one column, puts the new value in the same column
        /// 
        /// ColumnsToPerformActionOn AND ColumnsToReceiveDataFromAction should be the same
        /// </summary>
        public const string MULTIPLY_COLUMN = "multiply";

        /// <summary>
        /// Multiplies the value of one column, puts new value in new column, deletes old column.
        /// 
        /// ColumnsToPerformActionOn is the column you want to multiply AND the column that will be deleted 
        /// ColumnsToReceiveDataFromAction is the new column that will receive the new value
        /// </summary>
        public const string MULTIPLY_COLUMN_DELETE = "multiply-delete";

        /// <summary>
        /// Divides the value of one column
        /// 
        /// ColumnsToPerformActionOn should contain the name of the column you want to divide
        /// </summary>
        public const string DIVIDE_COLUMN = "divide";

        /// <summary>
        /// Divides the value of one column, puts new value in new column, deletes old column.
        /// 
        /// ColumnsToPerformActionOn should contain the name of the column you want to divide AND delete
        /// ColumnsToReceiveDataFromAction should contain the name of column you want to receive the new value
        /// </summary>
        public const string DIVIDE_COLUMN_DELETE = "divide-delete";

        /// <summary>
        /// Adds given value to column
        /// 
        /// ColumnsToPerformActionOn should be the name of the column that contains the value you want to add to
        /// ColumnsToReceiveDataFromAction should be the same name as ColumnsToPerformActionOn
        /// </summary>
        public const string ADD_COLUMN = "add";

        /// <summary>
        /// Adds given value to one column, puts new value in new column, deletes old column.
        /// 
        /// ColumnsToPerformActionOn should be the name of the column that contains the value you want to add to AND delete 
        /// ColumnsToReceiveDataFromAction should the column you want to insert the new value into
        /// </summary>
        public const string ADD_COLUMN_DELETE = "add-delete";

        /// <summary>
        /// Subtracts given value from one column
        /// 
        /// ColumnsToPerformActionOn should be the column you want to subtract from
        /// ColumnsToReceiveDataFromAction should be the same as ColumnsToPerformActionOn
        /// </summary>
        public const string SUBTRACT_COLUMN = "subtract";
        
        /// <summary>
        /// Subtracts given value from one column, puts new value in new column, deletes old column. 
        /// 
        /// ColumnsToPerformActionOn should be the column you want to subtract from AND delete
        /// ColumnsToReceiveDataFromAction should be the column you want the new value to be inserted into
        /// </summary>
        public const string SUBTRACT_COLUMN_DELETE = "subtract-delete";

        /// <summary>
        /// Deletes column
        /// 
        /// ColumnsToPerformActionOn is the column you want to delete
        /// ColumnsToReceiveDataFromAction should not have a value
        /// </summary>
        public const string DELETE_COLUMN = "delete-column";
    }
}
