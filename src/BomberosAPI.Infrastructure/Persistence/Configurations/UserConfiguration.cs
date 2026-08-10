using BomberosAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomberosAPI.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User");
        builder.HasKey(e => e.UserId);
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.InstitutionId).HasColumnName("institution_id");
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(150).IsRequired();
        builder.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(150).IsRequired();
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(e => e.AccountStatus).HasColumnName("account_status").HasMaxLength(50).IsRequired();
        builder.Property(e => e.EmailVerified).HasColumnName("email_verified").IsRequired().HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.LastAccessAt).HasColumnName("last_access_at");
        builder.HasIndex(e => e.Email).IsUnique();
    }
}
