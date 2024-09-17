using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DapperGenericRepository.Models.Parameters
{
    public record ConditionParams
    {
        public List<string> ConditionLeftSide { get; set; } = [];
        public List<ComparisonOperators> ComparisonOperators { get; set; } = [];
        public List<string> ConditionRightSide { get; set; } = [];
        public List<LogicOperators> LogicOperators { get; set; } = null;
    }

    public enum ComparisonOperators
    {
        [Display(Name = "=")]
        Equal,

        [Display(Name = "!=")]
        NotEqual,

        [Display(Name = ">")]
        GreaterThan,

        [Display(Name = ">=")]
        GreaterThanOrEqual,

        [Display(Name = "<")]
        LessThan,

        [Display(Name = "<=")]
        LessThanOrEqual
    }

    public enum LogicOperators
    {
        [Display(Name = "AND")]
        And,

        [Display(Name = "OR")]
        Or
    }
}
