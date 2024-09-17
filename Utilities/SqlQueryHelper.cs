using Dapper;
using DapperGenericRepository.Extensions;
using DapperGenericRepository.Models.Parameters;
using DapperGenericRepository.Models.Results;
using System.Text;

namespace DapperGenericRepository.Utilities
{
    public static class SqlQueryHelper
    {
        public static GenerateConditionResult GenerateWhereClause(ConditionParams conditionParams,
            IEnumerable<string> validColumns = null, int parametersIndex = 0)
        {
            #region Validation

            if (validColumns != null && conditionParams.ConditionLeftSide.Count >= 1
                && !conditionParams.ConditionLeftSide.All(validColumns.Contains))
                throw new Exception("Columns name are invalid");

            if (!(conditionParams.ConditionLeftSide.Count == conditionParams.ComparisonOperators.Count
                && conditionParams.ComparisonOperators.Count == conditionParams.ConditionRightSide.Count))
                throw new Exception("Invalid params");

            if ((conditionParams.ConditionLeftSide.Count > 1 || conditionParams.ComparisonOperators.Count > 1 || conditionParams.ConditionRightSide.Count > 1)
                && conditionParams.LogicOperators == null)
                throw new Exception("Logical Operator required for multiple conditions");

            if (conditionParams.LogicOperators != null && conditionParams.ConditionLeftSide.Count - 1 != conditionParams.LogicOperators.Count)
                throw new Exception("Wrong Logical Operator count");

            #endregion

            var whereClause = new StringBuilder(" WHERE ");
            DynamicParameters parameters = new();
            foreach (var (columnName, currentIndex) in conditionParams.ConditionLeftSide.Select((colName, index) => (colName, index)))
            {
                whereClause.Append($"[{columnName}] {conditionParams.ComparisonOperators[currentIndex].ToDisplay()} @rightSideValue_{currentIndex}_{parametersIndex}");

                if (conditionParams.ComparisonOperators.Count - 2 >= currentIndex)
                    whereClause.Append($" {conditionParams.LogicOperators[currentIndex].ToDisplay()} ");

                parameters.Add($"rightSideValue_{currentIndex}_{parametersIndex}", $"{conditionParams.ConditionRightSide[currentIndex]}");
            }

            return new GenerateConditionResult
            {
                WhereClause = whereClause.ToString(),
                Parameters = parameters,
            };
        }
    }
}
