using DapperGenericRepository.Models.Results;

namespace DapperGenericRepository.Extensions
{
    public static class GenericDataExtension
    {
        public static PaginationResult<TData> ToPaginationResult<TData>(this IEnumerable<TData> data, long totalDataCount, int pageSize)
        {
            return data != null
                ? new PaginationResult<TData>
                {
                    TotalCount = totalDataCount,
                    PageCount = pageSize != -1 ? (int)Math.Ceiling(totalDataCount / (double)pageSize) : 1,
                    Data = data
                }
                : new PaginationResult<TData>();
        }
    }
}
