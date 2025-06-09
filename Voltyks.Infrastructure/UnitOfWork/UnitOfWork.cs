using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Data;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly VoltyksDbContext _context;
        //private readonly SqlConnection _sqlFactory;
        private readonly ConcurrentDictionary<string, object> _repositories;

        //public IUserDapperRepository UserDapper { get; }

        public UnitOfWork(VoltyksDbContext context, SqlConnectionFactory sqlConnectionFactory )
        {
            _context = context;
            //_sqlFactory = sqlFactory;
            _repositories = new ConcurrentDictionary<string, object>();

            //UserDapper = new UserDapperRepository(_sqlFactory);
        }

        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : BaseEntity<TKey>
        {
            var typeName = typeof(TEntity).FullName!;
            return (IGenericRepository<TEntity, TKey>)_repositories.GetOrAdd(
                typeName,
                _ => new GenericRepository<TEntity, TKey>(_context)
            );
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }

}
