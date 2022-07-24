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
}