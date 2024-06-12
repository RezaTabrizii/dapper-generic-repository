using DapperGenericRepository.Contracts;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DapperGenericRepository.Settings
{
    public class SqlConnectionProvider(SqlSettings settings) : ISqlConnection
    {
        public IDbConnection CreateConnection()
        {
            return new SqlConnection(settings.ConnectionString);
        }
    }
}
