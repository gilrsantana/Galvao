using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Galvao.Infrastructure.Identity;
using Galvao.Application.Common.Interfaces;
using Galvao.Domain.Entities;

namespace Galvao.Infrastructure.Persistence;

public class GalvaoDbContext : IdentityDbContext<Account, Role, Guid>, IUnitOfWork
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<ShowroomItem> ShowroomItems => Set<ShowroomItem>();
    public DbSet<Article> Articles => Set<Article>();

    public GalvaoDbContext(DbContextOptions<GalvaoDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Custom Identity table mappings
        builder.Entity<IdentityUserRole<Guid>>().ToTable("AccountRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("AccountClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("AccountLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("AccountTokens");

        builder.ApplyConfigurationsFromAssembly(typeof(GalvaoDbContext).Assembly);
    }
}
