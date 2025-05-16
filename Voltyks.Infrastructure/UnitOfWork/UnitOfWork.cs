using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly VoltyksDbContext _context;
        //private readonly Dictionary<string, object> _repositories; // Null
        private readonly ConcurrentDictionary<string, object> _repositories; // Null

        public UnitOfWork(VoltyksDbContext context)
        {
            _context = context;
            _repositories = new ConcurrentDictionary<string, object>();
        }
        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : BaseEntity<TKey>
        {
            return (IGenericRepository<TEntity, TKey>)_repositories.GetOrAdd(typeof(TEntity).Name, new GenericRepository<TEntity, TKey>(_context));
        }


        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
