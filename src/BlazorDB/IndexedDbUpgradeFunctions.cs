// Author: Tyler Franklin
// New Code for Fork: BlazorDB-issue-11
namespace BlazorDB
{
    public struct IndexedDbUpgradeFunctions
    {
        // Splits the data in one column into two new columns
        // Must include two column names in the ColumnsToReceiveDataFromAction property
        public const string SPLIT_COLUMN = "split";
        // Multiplies the value of one column, puts the new value in the same column
        // ColumnsToPerformActionOn AND ColumnsToReceiveDataFromAction should be the same
        public const string MULTIPLY_COLUMN = "multiply";
        // Multiplies the value of one column, puts new value in new column, deletes old column.
        // ColumnsToPerformActionOn is the column you want to multiply AND the column that will be deleted
        // ColumnsToReceiveDataFromAction is the new column that will receive the new value
        public const string MULTIPLY_COLUMN_DELETE = "multiply-delete";
        // Divides the value of one column
        // ColumnsToPerformActionOn should contain the name of the column you want to divide
        public const string DIVIDE_COLUMN = "divide";
        // Divides the value of one column, puts new value in new column, deletes old column.
        // ColumnsToPerformActionOn should contain the name of the column you want to divide AND delete
        // ColumnsToReceiveDataFromAction should contain the name of column you want to receive the new value
        public const string DIVIDE_COLUMN_DELETE = "divide-delete";
        // Adds given value to column
        // ColumnsToPerformActionOn should be the name of the column that contains the value you want to add to
        // ColumnsToReceiveDataFromAction should be the same name as ColumnsToPerformActionOn
        public const string ADD_COLUMN = "add";
        // Adds given value to one column, puts new value in new column, deletes old column.
        // ColumnsToPerformActionOn should be the name of the column that contains the value you want to add to AND delete
        // ColumnsToReceiveDataFromAction should the column you want to insert the new value into
        public const string ADD_COLUMN_DELETE = "add-delete";
        // Subtracts given value from one column
        // ColumnsToPerformActionOn should be the column you want to subtract from
        // ColumnsToReceiveDataFromAction should be the same as ColumnsToPerformActionOn
        public const string SUBTRACT_COLUMN = "subtract";
        // Subtracts given value from one column, puts new value in new column, deletes old column.
        // ColumnsToPerformActionOn should be the column you want to subtract from AND delete
        // ColumnsToReceiveDataFromAction should be the column you want the new value to be inserted into
        public const string SUBTRACT_COLUMN_DELETE = "subtract-delete";
        // Deletes column
        // ColumnsToPerformActionOn is the column you want to delete
        // ColumnsToReceiveDataFromAction should not have a value
        public const string DELETE_COLUMN = "delete-column";
    }
}
