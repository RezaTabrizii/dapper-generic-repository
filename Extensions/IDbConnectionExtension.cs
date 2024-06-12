using Dapper;
using System.Data;
using static Dapper.SqlMapper;

namespace DapperGenericRepository.Extensions
{
    public static class IDbConnectionExtension
    {
        #region Synchronous Extensions

        public static IEnumerable<string> GetColumnsName(this IDbConnection dbConnection, string tableName)
        {
            string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
            return dbConnection.Query<string>(query, new { TableName = tableName });
        }

        public static IEnumerable<string> GetManyTablesColumnsName(this IDbConnection dbConnection, IEnumerable<string> tableNames)
        {
            List<string> columnNames = [];

            string query = "";
            var dynamicParameters = new DynamicParameters();
            foreach (var (tableName, currentIndex) in tableNames.Select((tbn, index) => (tbn, index)))
            {
                query += $" SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName{currentIndex};";
                dynamicParameters.Add($"@TableName{currentIndex}", tableName);
            }

            var data = dbConnection.QueryMultiple(query, dynamicParameters);
            for (var i = 1; i <= tableNames.Count();)
                columnNames.AddRange(data.Read<string>());

            return columnNames.Distinct();
        }

        #endregion

        #region Asynchronous Methods

        public static async Task<IEnumerable<string>> GetColumnsNameAsync(this IDbConnection dbConnection, string tableName)
        {
            string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
            return await dbConnection.QueryAsync<string>(query, new { TableName = tableName });
        }

        public static async Task<IEnumerable<string>> GetManyTablesColumnsNameAsync(this IDbConnection dbConnection, IEnumerable<string> tableNames)
        {
            List<string> columnNames = [];

            string query = "";
            var dynamicParameters = new DynamicParameters();
            foreach (var (tableName, currentIndex) in tableNames.Select((tbn, index) => (tbn, index)))
            {
                query += $" SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName{currentIndex};";
                dynamicParameters.Add($"@TableName{currentIndex}", tableName);
            }

            var data = await dbConnection.QueryMultipleAsync(query, dynamicParameters);
            for (var i = 1; i <= tableNames.Count();)
                columnNames.AddRange(data.Read<string>());

            return columnNames.Distinct();
        }

        #endregion
    }
}