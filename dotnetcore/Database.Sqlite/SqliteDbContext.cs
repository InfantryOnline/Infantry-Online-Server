using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Sqlite
{
    public partial class SqliteDbContext : DbContext
    {
        public string DbPath { get; }

        public SqliteDbContext()
        {
            DbPath = "database.db";
        }

        public SqliteDbContext(string dbpath)
        {
            DbPath = dbpath;
        }

        public virtual DbSet<Account> Accounts { get; set; }

        public virtual DbSet<Alias> Aliases { get; set; }

        public virtual DbSet<Ban> Bans { get; set; }

        public virtual DbSet<Helpcall> Helpcalls { get; set; }

        public virtual DbSet<History> Histories { get; set; }

        public virtual DbSet<Player> Players { get; set; }

        public virtual DbSet<ResetToken> ResetTokens { get; set; }

        public virtual DbSet<Squad> Squads { get; set; }

        public virtual DbSet<Stat> Stats { get; set; }

        public virtual DbSet<StatsDaily> StatsDailies { get; set; }

        public virtual DbSet<StatsMonthly> StatsMonthlies { get; set; }

        public virtual DbSet<StatsWeekly> StatsWeeklies { get; set; }

        public virtual DbSet<StatsYearly> StatsYearlies { get; set; }

        public virtual DbSet<Zone> Zones { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
