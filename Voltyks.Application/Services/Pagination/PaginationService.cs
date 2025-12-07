using Microsoft.EntityFrameworkCore;
using Voltyks.Application.Interfaces.Pagination;
using Voltyks.Core.DTOs.Common;

namespace Voltyks.Application.Services.Pagination
{
    /// <summary>
    /// Reusable pagination service that can be applied to any IQueryable
    /// </summary>
    public class PaginationService : IPaginationService
    {
        /// <summary>
        /// Applies pagination to an IQueryable and returns a PagedResult (async for EF Core)
        /// </summary>
        public async Task<PagedResult<T>> PaginateAsync<T>(
            IQueryable<T> query,
            PaginationParams paginationParams,
            CancellationToken ct = default)
        {
            var totalCount = await query.CountAsync(ct);

            var items = await query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync(ct);

            return new PagedResult<T>(items, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
        }

        /// <summary>
        /// Applies pagination to an IQueryable (synchronous for in-memory collections)
        /// </summary>
        public PagedResult<T> Paginate<T>(
            IQueryable<T> query,
            PaginationParams paginationParams)
        {
            var totalCount = query.Count();

            var items = query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToList();

            return new PagedResult<T>(items, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
        }

        /// <summary>
        /// Wraps a pre-fetched list into a PagedResult
        /// </summary>
        public PagedResult<T> Paginate<T>(
            List<T> items,
            int totalCount,
            PaginationParams paginationParams)
        {
            return new PagedResult<T>(items, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
        }
    }
}
