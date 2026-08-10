using BomberosAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BomberosAPI.Infrastructure.Persistence.Configurations;

public class AccountActivationTokenConfiguration : IEntityTypeConfiguration<AccountActivationToken>
{
    public void Configure(EntityTypeBuilder<AccountActivationToken> builder)
    {
        builder.ToTable("AccountActivationToken");
        builder.HasKey(e => e.AccountActivationTokenId);
        builder.Property(e => e.AccountActivationTokenId).HasColumnName("account_activation_token_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        builder.Property(e => e.UsedAt).HasColumnName("used_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
    }
}
