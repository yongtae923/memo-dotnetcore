using Microsoft.EntityFrameworkCore;

namespace AblyAPI.Services;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }
}