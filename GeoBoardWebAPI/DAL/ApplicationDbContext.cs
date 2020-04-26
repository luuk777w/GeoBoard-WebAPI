using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GeoBoardWebAPI.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using System;

namespace GeoBoardWebAPI.DAL
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardElement> BoardElements { get; set; } 
        public DbSet<Country> Countries { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Snapshot> Snapshots { get; set; }
        public DbSet<SnapshotElement> SnapshotElements { get; set; }
        public DbSet<UserBoard> UserBoards { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }
        public DbSet<UserSnapshot> UserSnapshots { get; set; }

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
                  .WithMany(b => b.Users);

                ub.HasOne(ub => ub.User)
                    .WithMany(u => u.Boards);
            });

            builder.Entity<SnapshotSnapshotElement>(us =>
            {
                // Define the composite primary key.
                us.HasKey(us => new { us.SnapshotId, us.SnapshotElementId });

                us.HasOne(us => us.Snapshot)
                  .WithMany();

                us.HasOne(us => us.User)
                  .WithMany(u => u.Snapshots);
            });
        }
    }
}
