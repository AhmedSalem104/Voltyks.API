
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;
using System.Reflection.Emit;
using Voltyks.Persistence.Entities.Main.Paymob;

namespace Voltyks.Persistence.Data.Configurations
{
    public class UserSavedCardConfiguration : IEntityTypeConfiguration<UserSavedCard>
    {
        public void Configure(EntityTypeBuilder<UserSavedCard> builder)
        {

            // Unique Index على UserId + Token
            builder.HasIndex(x => new { x.UserId, x.Token })
                   .IsUnique();

            // لو عايز تضبط طول أو Required لحقول معينة ممكن تضيف:
            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.Token).IsRequired();
            builder.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        }
    }


}
