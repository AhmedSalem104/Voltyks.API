
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Infrastructure
{

    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
      where TEntity : BaseEntity<TKey>
    {
        private readonly VoltyksDbContext _context;

        public GenericRepository(VoltyksDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? filter = null, bool trackChanges = false)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            if (!trackChanges)
                query = query.AsNoTracking();  // تفعيل AsNoTracking عند عدم تتبع التغييرات

            if (filter is not null)
                query = query.Where(filter);

            return await query.ToListAsync();
        }
        public async Task<TEntity?> GetAsync(TKey id)
        {
            return await _context.Set<TEntity>().FindAsync(id);
        }
        public async Task<IEnumerable<TEntity>> GetAllWithIncludeAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        bool trackChanges = false,
        params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            if (!trackChanges)
                query = query.AsNoTracking();

            if (filter is not null)
                query = query.Where(filter);

            foreach (var include in includes)
                query = query.Include(include);

            return await query.ToListAsync();
        }
        public async Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool trackChanges = false)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(predicate);
        }
        public async Task AddAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
        }
        public void Update(TEntity entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
        }
        public void Delete(TEntity entity)
        {
            var existingEntity = _context.Set<TEntity>().Find(entity.Id);
            if (existingEntity != null)
            {
                _context.Set<TEntity>().Remove(existingEntity);
            }
        }
        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _context.Set<TEntity>().AnyAsync(predicate);
        }
        public IQueryable<TEntity> Query(bool trackChanges = false)
        {
            var q = _context.Set<TEntity>().AsQueryable();
            return trackChanges ? q : q.AsNoTracking();
        }
        public async Task ExecuteInTransactionAsync(Func<Task> action, IsolationLevel level = IsolationLevel.ReadCommitted)
        {
            using var tx = await _context.Database.BeginTransactionAsync(level);
            try
            {
                await action();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

    }

}
