using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Application.Interfaces.Paymob
{
    
    public interface IPaymobAuthTokenProvider
    {
        Task<string> GetAsync(CancellationToken ct = default);
        Task InvalidateAsync();
    }

}
