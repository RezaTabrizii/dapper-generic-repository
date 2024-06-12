namespace DapperGenericRepository.Models.Results
{
    public class PaginationResult<TEntity>
    {
        public int PageCount { get; set; }
        public long TotalCount { get; set; }
        public IEnumerable<TEntity> Data { get; set; }
    }
}
