using Microsoft.EntityFrameworkCore;

namespace auth.Models;

public class AuthorizationContext : DbContext
{
    public AuthorizationContext(DbContextOptions<AuthorizationContext> options)
        : base(options)
    {
        
    }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<AuthorizationGrant> AuthorizationGrants { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; }  = null!;
    public DbSet<Client> Clients { get; set; } = null!;


}