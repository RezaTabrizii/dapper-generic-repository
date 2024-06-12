namespace DapperGenericRepository.Models.Parameters
{
    public record ConditionParams
    {
        public List<string> ConditionLeftSide { get; set; } = [];
        public List<string> ComparisonOperators { get; set; } = [];
        public List<string> ConditionRightSide { get; set; } = [];
        public List<string> LogicOperators { get; set; } = null;
    }
}
