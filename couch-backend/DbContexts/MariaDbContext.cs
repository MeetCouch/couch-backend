using couch_backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace couch_backend.DbContexts
{
    public class MariaDbContext : IdentityDbContext<User, Role, Guid>
    {
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<Subscription> Subscriptions { get; set; }
        public virtual DbSet<IdentityUserLogin<Guid>> UserLogins { get; set; }
        public virtual DbSet<User> Users { get; set; }

        public MariaDbContext(DbContextOptions<MariaDbContext> options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityUserLogin<Guid>>()
                .ToTable("UserLogins")
                .HasKey(x => new { x.UserId, x.ProviderKey });

            modelBuilder.Entity<IdentityUserRole<Guid>>()
                .ToTable("UserRoles")
                .HasKey(x => new { x.UserId, x.RoleId });

            modelBuilder.Entity<Subscription>()
                .HasKey(bc => new { bc.Email });

            modelBuilder.Entity<Subscription>()
                .HasIndex(bc => new { bc.Email })
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(bc => new { bc.UserName })
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(bc => new { bc.PhoneNumber })
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(bc => new { bc.Email })
                .IsUnique();

        }
    }
}
