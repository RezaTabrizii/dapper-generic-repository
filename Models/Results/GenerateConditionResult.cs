using Dapper;

namespace DapperGenericRepository.Models.Results
{
    public record struct GenerateConditionResult
    {
        public string WhereClause { get; set; }
        public DynamicParameters Parameters { get; set; }
    }
}
