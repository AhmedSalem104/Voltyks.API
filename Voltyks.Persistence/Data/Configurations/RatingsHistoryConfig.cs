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
    public class RatingsHistoryConfig : IEntityTypeConfiguration<RatingsHistory>
    {
        public void Configure(EntityTypeBuilder<RatingsHistory> b)
        {
            b.ToTable("RatingsHistory");
            b.HasKey(x => x.Id);
            b.Property(x => x.Stars).IsRequired();
            b.HasIndex(x => new { x.ProcessId, x.RaterUserId }).IsUnique(); // يمنع التقييم مرتين لنفس العملية من نفس المقيّم
        }
    }

}
