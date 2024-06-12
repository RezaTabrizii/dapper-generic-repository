namespace DapperGenericRepository.Models.Base
{
    public abstract class TableBase
    {
        public DateTime CreatedMoment { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedMoment { get; set; } = null;
    }
}
