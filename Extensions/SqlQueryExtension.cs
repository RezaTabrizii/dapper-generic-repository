using Dapper;
using DapperGenericRepository.Models.Parameters;
using DapperGenericRepository.Utilities;

namespace DapperGenericRepository.Extensions
{
    public static class SqlQueryExtension
    {
        public static void ApplyFilter(this string query, ref DynamicParameters param, List<string> conditionLeftSide, List<ComparisonOperators> comparisonOperators,
            List<string> conditionRightSide, out string modifiedQuery, List<LogicOperators> logicalOperators = null)
        {
            query = $"SELECT * FROM ({query}) As BaseQuery ";

            if (conditionLeftSide.Count <= 0 || comparisonOperators.Count <= 0 || conditionRightSide.Count <= 0)
            {
                modifiedQuery = query;
                return;
            }

            var condition = SqlQueryHelper.GenerateWhereClause(new ConditionParams
            {
                ConditionLeftSide = conditionLeftSide,
                ComparisonOperators = comparisonOperators,
                ConditionRightSide = conditionRightSide,
                LogicOperators = logicalOperators
            });

            query += condition.WhereClause;
            param.AddDynamicParams(condition.Parameters);

            modifiedQuery = query;
        }

        public static void ApplySorting(this string query, string orderByColumnName, bool orderByDescending, out string modifiedQuery)
        {
            if (string.IsNullOrEmpty(orderByColumnName))
                orderByColumnName = "CreatedMoment";

            query += $" ORDER BY [{orderByColumnName}] {(orderByDescending ? "DESC" : "ASC")}";

            modifiedQuery = query;
        }

        public static void ApplyPaging(this string query, ref DynamicParameters param, int page, int pageSize, out string modifiedQuery)
        {
            var queryWithoutOrder = query.Split("ORDER")[0];

            var index = page < 1 ? 1 : page;
            var size = pageSize < 1 && pageSize != -1 ? 10 : pageSize;
            if (size != -1)
            {
                if (!query.Contains("offset", StringComparison.CurrentCultureIgnoreCase))
                    query += " OFFSET @Offset ROWS FETCH NEXT @Next ROWS ONLY;" +
                        $"; SELECT COUNT(*) FROM ({queryWithoutOrder}) As RowsCount";

                param.Add("@Offset", (index - 1) * size);
                param.Add("@Next", size);
            }
            else
                query += $"; SELECT COUNT(*) FROM ({queryWithoutOrder}) As RowsCount";

            modifiedQuery = query;
        }
    }
}
