using System.Data;

namespace DapperGenericRepository.Contracts
{
    public interface ISqlConnection
    {
        IDbConnection CreateConnection();
    }
}
