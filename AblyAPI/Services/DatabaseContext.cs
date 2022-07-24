using AblyAPI.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace AblyAPI.Services;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }
    
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Credential> Credentials { get; set; }
    public DbSet<AccessToken> AccessTokens { get; set; }
    public DbSet<VerificationCode> VerificationCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>()
            .HasKey(account => account.Id);
        modelBuilder.Entity<Account>()
            .HasIndex(account => account.CreatedAt);

        modelBuilder.Entity<Credential>()
            .HasKey(credential => new {credential.AccountId, credential.Provider});

        modelBuilder.Entity<AccessToken>()
            .HasKey(token => new {token.AccountId, token.Token});

        modelBuilder.Entity<VerificationCode>()
            .HasKey(code => code.Id);
        modelBuilder.Entity<VerificationCode>()
            .HasIndex(code => code.Id);

        base.OnModelCreating(modelBuilder);
    }
}