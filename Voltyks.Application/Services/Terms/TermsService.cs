using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voltyks.Application.Interfaces.Terms;
using Voltyks.Core.DTOs.Terms;
using Voltyks.Persistence.Data;

namespace Voltyks.Application.Services.Terms
{
    public class TermsService : ITermsService
    {
        private readonly VoltyksDbContext _db;

        public TermsService(VoltyksDbContext db)
        {
            _db = db;
        }

        public async Task<TermsResponseDto?> GetAsync(string lang, int? version, CancellationToken ct = default)
        {
            lang = (lang?.ToLowerInvariant()) switch { "ar" => "ar", _ => "en" };

            var q = _db.termsDocuments.AsNoTracking();

            var doc = version.HasValue
                ? await q.FirstOrDefaultAsync(x => x.VersionNumber == version && x.Lang == lang, ct)
                : await q.Where(x => x.IsActive && x.Lang == lang)
                         .OrderByDescending(x => x.VersionNumber)
                         .FirstOrDefaultAsync(ct);

            if (doc is null) return null;

            return new TermsResponseDto
            {
                Version = doc.VersionNumber,
                Lang = doc.Lang,
                PublishedAt = doc.PublishedAt,
                Content = JsonSerializer.Deserialize<object>(doc.PayloadJson)!
            };
        }
    }
}
