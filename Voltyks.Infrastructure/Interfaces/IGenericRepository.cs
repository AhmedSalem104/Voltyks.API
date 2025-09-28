using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Infrastructure
{
    public interface IGenericRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? filter = null, bool trackChanges = false);
        Task<IEnumerable<TEntity>> GetAllWithIncludeAsync(
              Expression<Func<TEntity, bool>>? filter = null,
              bool trackChanges = false,
              params Expression<Func<TEntity, object>>[] includes);
        Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool trackChanges = false);
        Task<TEntity?> GetAsync(TKey id);
        Task AddAsync(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
        IQueryable<TEntity> Query(bool trackChanges = false);

    }
}
