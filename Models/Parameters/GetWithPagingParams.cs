namespace DapperGenericRepository.Models.Parameters
{
    public class GetWithPagingParams
    {
        public string SqlQuery { get; set; }
        public object Parameters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<string> ConditionLeftSide { get; set; } = [];
        public List<string> ComparisonOperators { get; set; } = [];
        public List<string> ConditionRightSide { get; set; } = [];
        public List<string> LogicOperators { get; set; } = null;
        public bool? OrderByDescending { get; set; } = null;
        public string OrderColumnName { get; set; } = null;
    }
}
