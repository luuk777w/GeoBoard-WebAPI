using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GeoBoardWebAPI.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using System;

namespace GeoBoardWebAPI.DAL
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>
    {
        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardElement> BoardElements { get; set; } 
        public DbSet<Country> Countries { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Snapshot> Snapshots { get; set; }
        public DbSet<SnapshotElement> SnapshotElements { get; set; }
        public DbSet<SnapshotSnapshotElement> SnapshotSnapshotElement { get; set; }
        public DbSet<UserBoard> UserBoards { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserBoard>(ub =>
            {
                // Define the composite primary key.
                ub.HasKey(ub => new { ub.BoardId, ub.UserId });

                ub.HasOne(ub => ub.Board)
                  .WithMany(b => b.Users)
                  .HasForeignKey(ub => ub.BoardId);

                ub.HasOne(ub => ub.User)
                    .WithMany(u => u.Boards)
                    .HasForeignKey(ub => ub.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<SnapshotSnapshotElement>(sse =>
            {
                // Define the composite primary key.
                sse.HasKey(sse => new { sse.SnapshotId, sse.SnapshotElementId });

                sse.HasOne(sse => sse.Element)
                    .WithMany(e => e.SnapshotSnapshotElement)
                    .HasForeignKey(sse => sse.SnapshotElementId);

                sse.HasOne(sse => sse.Snapshot)
                    .WithMany(s => s.Elements)
                    .HasForeignKey(sse => sse.SnapshotId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
