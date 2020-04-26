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

            builder.Entity<Board>()
                .HasOne(b => b.CreatedBy)
                .WithMany(u => u.CreatedBoards);

            builder.Entity<BoardElement>()
                .HasOne(be => be.Board)
                .WithMany(b => b.Elements);

            builder.Entity<Person>()
                .HasOne(p => p.Country)
                .WithMany();

            builder.Entity<Snapshot>()
                .HasOne(s => s.CreatedBy)
                .WithMany(u => u.CreatedSnapshots);

            builder.Entity<SnapshotElement>()
                .HasOne(se => se.Snapshot)
                .WithMany(s => s.Elements);

            //builder.Entity<User>()
            //    .HasOne(u => u.Person)
            //    .WithOne();

            builder.Entity<UserBoard>(ub =>
            {
                // Define the composite primary key.
                ub.HasKey(ub => new { ub.BoardId, ub.UserId });

                ub.HasOne(ub => ub.Board)
                  .WithMany(b => b.Users);

                ub.HasOne(ub => ub.User)
                    .WithMany(u => u.Boards);
            });

            //builder.Entity<UserSetting>(us =>
            //{

            //});

            //builder.Entity<UserSnapshot>(us =>
            //{
            //    // Define the composite primary key.
            //    us.HasKey(us => new { us.UserId, us.SnapshotId });
            //});
        }
    }
}
