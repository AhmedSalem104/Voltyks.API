using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Terms;

namespace Voltyks.Application.Interfaces.Terms
{
    public interface ITermsService
    {
    
        Task<TermsResponseDto?> GetAsync(string lang, int? version, CancellationToken ct = default);
    }
}
