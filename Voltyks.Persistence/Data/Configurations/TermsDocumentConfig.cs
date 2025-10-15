using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class TermsDocumentConfig : IEntityTypeConfiguration<TermsDocument>
    {
        public void Configure(EntityTypeBuilder<TermsDocument> b)
        {
            b.ToTable("TermsDocuments");
            b.HasKey(x => x.Id);

            b.Property(x => x.VersionNumber).IsRequired();
            b.Property(x => x.Lang).IsRequired().HasMaxLength(5);
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.PublishedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.PayloadJson).IsRequired(); // nvarchar(max)

            b.HasIndex(x => new { x.VersionNumber, x.Lang }).IsUnique();
            b.HasIndex(x => new { x.IsActive, x.Lang });
        }
    }
}
