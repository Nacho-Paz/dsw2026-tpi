using Dsw2026Tpi.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Data.Identity;

//CHECK: Ready
public class AuthenticationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options)
            : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new ApplicationUserConfiguration());

        builder.Entity<ApplicationUser>(b => { b.ToTable("Users"); });
        builder.Entity<ApplicationRole>(b => { b.ToTable("Roles"); });
        builder.Entity<IdentityUserRole<Guid>>(b => { b.ToTable("UsersRoles"); });
        builder.Entity<IdentityUserClaim<Guid>>(b => { b.ToTable("UsersClaims"); });
        builder.Entity<IdentityUserLogin<Guid>>(b => { b.ToTable("UsersLogins"); });
        builder.Entity<IdentityRoleClaim<Guid>>(b => { b.ToTable("RolesClaims"); });
        builder.Entity<IdentityUserToken<Guid>>(b => { b.ToTable("UsersTokens"); });
    }
}
