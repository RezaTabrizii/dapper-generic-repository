using Dapper;
using DapperGenericRepository.Contracts;
using System.Data;
using System.Text;

namespace DapperGenericRepository.Extensions
{
    public static class ISqlConnectionExtension
    {
        #region Synchronous Extensions

        public static IEnumerable<string> GetColumnsName(this ISqlConnection sqlConnection, string tableName)
        {
            string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";

            IEnumerable<string> columnsName;
            using (var dbConnection = sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                columnsName = dbConnection.Query<string>(query, new { TableName = tableName });
            }

            return columnsName;
        }

        public static IEnumerable<string> GetManyTablesColumnsName(this ISqlConnection sqlConnection, IEnumerable<string> tableNames)
        {
            List<string> columnNames = [];

            StringBuilder queryBuilder = new();
            var dynamicParameters = new DynamicParameters();
            foreach (var (tableName, currentIndex) in tableNames.Select((tbn, index) => (tbn, index)))
            {
                queryBuilder.Append($" SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName{currentIndex};");
                dynamicParameters.Add($"@TableName{currentIndex}", tableName);
            }

            using (var dbConnection = sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = dbConnection.QueryMultiple(queryBuilder.ToString(), dynamicParameters);
                for (var i = 1; i <= tableNames.Count();)
                    columnNames.AddRange(data.Read<string>());
            }

            return columnNames.Distinct();
        }

        #endregion

        #region Asynchronous Methods

        public static async Task<IEnumerable<string>> GetColumnsNameAsync(this ISqlConnection sqlConnection, string tableName)
        {
            string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";

            IEnumerable<string> columnsName;
            using (var dbConnection = sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                columnsName = await dbConnection.QueryAsync<string>(query, new { TableName = tableName });
            }

            return columnsName;
        }

        public static async Task<IEnumerable<string>> GetManyTablesColumnsNameAsync(this ISqlConnection sqlConnection, IEnumerable<string> tableNames)
        {
            List<string> columnNames = [];

            StringBuilder queryBuilder = new();
            var dynamicParameters = new DynamicParameters();
            foreach (var (tableName, currentIndex) in tableNames.Select((tbn, index) => (tbn, index)))
            {
                queryBuilder.Append($" SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName{currentIndex};");
                dynamicParameters.Add($"@TableName{currentIndex}", tableName);
            }

            using (var dbConnection = sqlConnection.CreateConnection())
            {
                dbConnection.Open();
                var data = await dbConnection.QueryMultipleAsync(queryBuilder.ToString(), dynamicParameters);
                for (var i = 1; i <= tableNames.Count();)
                    columnNames.AddRange(data.Read<string>());
            }

            return columnNames.Distinct();
        }

        #endregion
    }
}
