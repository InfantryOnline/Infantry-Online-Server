using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Sqlite
{
    public partial class SqliteDbContext : InfantryDbContext
    {
        public SqliteDbContext(DbContextOptions<SqliteDbContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(entity =>
            {
                entity.Property(e => e.Name).UseCollation("NOCASE");
                entity.Property(e => e.Password).UseCollation("NOCASE");
                entity.Property(e => e.Ticket).UseCollation("NOCASE");
                entity.Property(e => e.Email).UseCollation("NOCASE");
                entity.Property(e => e.IpAddress).UseCollation("NOCASE");

                entity.HasIndex(e => e.Ticket);
            });

            modelBuilder.Entity<Alias>(entity =>
            {
                entity.Property(e => e.Name).UseCollation("NOCASE");
                entity.Property(e => e.IpAddress).UseCollation("NOCASE");
            });

            modelBuilder.Entity<Ban>(entity =>
            {
                entity.Property(e => e.IpAddress).UseCollation("NOCASE");
                entity.Property(e => e.Reason).UseCollation("NOCASE");
                entity.Property(e => e.Name).UseCollation("NOCASE");

                entity.HasIndex(e => e.AccountId);
                entity.HasIndex(e => e.IpAddress);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Uid1);
                entity.HasIndex(e => e.Uid2);
                entity.HasIndex(e => e.Uid3);
            });

            modelBuilder.Entity<Helpcall>(entity =>
            {
                entity.Property(e => e.Sender).UseCollation("NOCASE");
                entity.Property(e => e.Arena).UseCollation("NOCASE");
                entity.Property(e => e.Zone).UseCollation("NOCASE");
                entity.Property(e => e.Reason).UseCollation("NOCASE");
            });

            modelBuilder.Entity<History>(entity =>
            {
                entity.Property(e => e.Sender).UseCollation("NOCASE");
                entity.Property(e => e.Recipient).UseCollation("NOCASE");
                entity.Property(e => e.Zone).UseCollation("NOCASE");
                entity.Property(e => e.Arena).UseCollation("NOCASE");
                entity.Property(e => e.Command).UseCollation("NOCASE");
            });

            modelBuilder.Entity<ResetToken>(entity =>
            {
                entity.Property(e => e.Name).UseCollation("NOCASE");
                entity.Property(e => e.Token).UseCollation("NOCASE");
            });

            modelBuilder.Entity<Squad>(entity =>
            {
                entity.Property(e => e.Name).UseCollation("NOCASE");
                entity.Property(e => e.Password).UseCollation("NOCASE");
            });

            modelBuilder.Entity<Zmod>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasIndex(e => new { e.Account, e.Zone }).IsUnique().HasDatabaseName("zmod_uc_account_zone");

                entity.HasOne(d => d.AccountNavigation).WithMany(p => p.Zmods)
                    .HasForeignKey(d => d.Account)
                    .HasConstraintName("zmod_account");

                entity.HasOne(d => d.ZoneNavigation).WithMany(p => p.Zmods)
                    .HasForeignKey(d => d.Zone)
                    .HasConstraintName("zmod_zone");
            });

            modelBuilder.Entity<Zone>(entity =>
            {
                entity.Property(e => e.Password).UseCollation("NOCASE");
                entity.Property(e => e.Name).UseCollation("NOCASE");
                entity.Property(e => e.Description).UseCollation("NOCASE");
                entity.Property(e => e.Notice).UseCollation("NOCASE");
                entity.Property(e => e.Ip).UseCollation("NOCASE");

                entity.HasIndex(e => e.OldId).IsUnique().HasDatabaseName("UQ_zone_old_id");

                entity.Property(e => e.OldId).HasColumnName("old_id");
            });
        }
    }
}
