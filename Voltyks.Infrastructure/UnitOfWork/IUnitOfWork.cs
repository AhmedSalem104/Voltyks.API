using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Infrastructure.UnitOfWork
{


    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();

        // Generic EF Repositories
        IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : BaseEntity<TKey>;

       

    }

}
