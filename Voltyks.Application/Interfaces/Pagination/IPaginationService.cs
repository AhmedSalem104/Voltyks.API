using Voltyks.Core.DTOs.Common;

namespace Voltyks.Application.Interfaces.Pagination
{
    /// <summary>
    /// Generic pagination service for applying pagination to any IQueryable source
    /// </summary>
    public interface IPaginationService
    {
        /// <summary>
        /// Applies pagination to an IQueryable and returns a PagedResult
        /// </summary>
        /// <typeparam name="T">The type of items being paginated</typeparam>
        /// <param name="query">The IQueryable source</param>
        /// <param name="paginationParams">Pagination parameters (page number, page size)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>PagedResult containing items and pagination metadata</returns>
        Task<PagedResult<T>> PaginateAsync<T>(
            IQueryable<T> query,
            PaginationParams paginationParams,
            CancellationToken ct = default);

        /// <summary>
        /// Applies pagination to an IQueryable and returns a PagedResult (synchronous version for in-memory collections)
        /// </summary>
        PagedResult<T> Paginate<T>(
            IQueryable<T> query,
            PaginationParams paginationParams);

        /// <summary>
        /// Applies pagination to a List and returns a PagedResult
        /// </summary>
        PagedResult<T> Paginate<T>(
            List<T> items,
            int totalCount,
            PaginationParams paginationParams);
    }
}
