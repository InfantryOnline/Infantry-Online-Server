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

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


        }
    }
}
