using CheckersLone.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckersLone.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Game> Games { get; set; }

        public DbSet<GameStats> GameStats { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        { optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=CheckersLoneDb;Trusted_Connection=True;"); }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>().HasData(
                new Player { Id = 1, Name = "Player1", Password = "123" },
                new Player { Id = 2, Name = "Player2", Password = "231" },
                new Player { Id = 3, Name = "TestPlayer", Password = "TestPassword" }
            );
        }
    }
}
