using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;

namespace Database.SqlServer;

public partial class SqlServerDbContext : DbContext
{
    private readonly string _connectionString;

    public SqlServerDbContext()
    {
    }

    public SqlServerDbContext(DbContextOptions<SqlServerDbContext> options)
        : base(options)
    {

    }

    //public DataContext(string connectionString)
    //{
    //    _connectionString = connectionString;
    //}

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

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    => optionsBuilder
    //    .UseLazyLoadingProxies()
    //    .UseSqlServer(
    //        _connectionString,

    //        // By default, split queries that contain .Include() joins.
    //        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK_dbo.account");

            entity.ToTable("account");

            entity.Property(e => e.AccountId).HasColumnName("id");
            entity.Property(e => e.DateCreated)
                .HasColumnType("datetime")
                .HasColumnName("dateCreated");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.ForumId).HasColumnName("forumID");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("IPAddress");
            entity.Property(e => e.LastAccess)
                .HasColumnType("datetime")
                .HasColumnName("lastAccess");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.Permission).HasColumnName("permission");
            entity.Property(e => e.Ticket)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("ticket");
        });

        modelBuilder.Entity<Alias>(entity =>
        {
            entity.HasKey(e => e.AliasId).HasName("PK_dbo.alias");

            entity.ToTable("alias");

            entity.Property(e => e.AliasId).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account");
            entity.Property(e => e.Creation)
                .HasColumnType("datetime")
                .HasColumnName("creation");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("IPAddress");
            entity.Property(e => e.LastAccess)
                .HasColumnType("datetime")
                .HasColumnName("lastAccess");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Stealth).HasColumnName("stealth");
            entity.Property(e => e.TimePlayed).HasColumnName("timeplayed");

            entity.HasOne(d => d.AccountNavigation).WithMany(p => p.Aliases)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AliasAccount");
        });

        modelBuilder.Entity<Ban>(entity =>
        {
            entity.ToTable("ban");

            entity.Property(e => e.BanId).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account");
            entity.Property(e => e.Created)
                .HasColumnType("datetime")
                .HasColumnName("created");
            entity.Property(e => e.Expires)
                .HasColumnType("datetime")
                .HasColumnName("expires");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("IPAddress");
            entity.Property(e => e.Name)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Reason)
                .IsUnicode(false)
                .HasColumnName("reason");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Uid1).HasColumnName("uid1");
            entity.Property(e => e.Uid2).HasColumnName("uid2");
            entity.Property(e => e.Uid3).HasColumnName("uid3");
            entity.Property(e => e.ZoneId).HasColumnName("zone");
        });

        modelBuilder.Entity<Helpcall>(entity =>
        {
            entity.ToTable("helpcall");

            entity.Property(e => e.HelpCallId).HasColumnName("id");
            entity.Property(e => e.Arena)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("arena");
            entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.Reason)
                .HasColumnType("text")
                .HasColumnName("reason");
            entity.Property(e => e.Sender)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sender");
            entity.Property(e => e.Zone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("zone");
        });

        modelBuilder.Entity<History>(entity =>
        {
            entity.ToTable("history");

            entity.Property(e => e.HistoryId).HasColumnName("id");
            entity.Property(e => e.Arena)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("arena");
            entity.Property(e => e.Command)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("command");
            entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.Recipient)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("recipient");
            entity.Property(e => e.Sender)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sender");
            entity.Property(e => e.Zone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("zone");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.PlayerId).HasName("PK_dbo.player");

            entity.ToTable("player");

            entity.Property(e => e.PlayerId).HasColumnName("id");
            entity.Property(e => e.AliasId).HasColumnName("alias");
            entity.Property(e => e.Banner)
                .HasMaxLength(8000)
                .HasColumnName("banner");
            entity.Property(e => e.Inventory)
                .HasMaxLength(512)
                .HasColumnName("inventory");
            entity.Property(e => e.LastAccess)
                .HasColumnType("datetime")
                .HasColumnName("lastAccess");
            entity.Property(e => e.Permission).HasColumnName("permission");
            entity.Property(e => e.Skills)
                .HasMaxLength(512)
                .HasColumnName("skills");
            entity.Property(e => e.SquadId).HasColumnName("squad");
            entity.Property(e => e.StatId).HasColumnName("stats");
            entity.Property(e => e.ZoneId).HasColumnName("zone");

            entity.HasOne(d => d.AliasNavigation).WithMany(p => p.Players)
                .HasForeignKey(d => d.AliasId)
                .HasConstraintName("alias_player");

            entity.HasOne(d => d.SquadNavigation).WithMany(p => p.Players)
                .HasForeignKey(d => d.SquadId)
                .HasConstraintName("FK_PlayerSquad");

            entity.HasOne(d => d.StatsNavigation).WithMany(p => p.Players)
                .HasForeignKey(d => d.StatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stats_player");

            entity.HasOne(d => d.ZoneNavigation).WithMany(p => p.Players)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("zone_player");
        });

        modelBuilder.Entity<ResetToken>(entity =>
        {
            entity.ToTable("resetToken");

            entity.Property(e => e.ResetTokenId).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account");
            entity.Property(e => e.ExpireDate)
                .HasColumnType("datetime")
                .HasColumnName("expireDate");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Token)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("token");
            entity.Property(e => e.TokenUsed).HasColumnName("tokenUsed");

            entity.HasOne(d => d.AccountNavigation).WithMany(p => p.ResetTokens)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("account_resetToken");
        });

        modelBuilder.Entity<Squad>(entity =>
        {
            entity.HasKey(e => e.SquadId).HasName("PK_dbo.squad");

            entity.ToTable("squad");

            entity.Property(e => e.SquadId).HasColumnName("id");
            entity.Property(e => e.DateCreated)
                .HasColumnType("datetime")
                .HasColumnName("dateCreated");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.OwnerPlayerId).HasColumnName("owner");
            entity.Property(e => e.Password)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.ZoneId).HasColumnName("zone");
        });

        modelBuilder.Entity<Stat>(entity =>
        {
            entity.HasKey(e => e.StatId).HasName("PK_dbo.stats");

            entity.ToTable("stats");

            entity.Property(e => e.StatId).HasColumnName("id");
            entity.Property(e => e.AssistPoints).HasColumnName("assistPoints");
            entity.Property(e => e.BonusPoints).HasColumnName("bonusPoints");
            entity.Property(e => e.Cash).HasColumnName("cash");
            entity.Property(e => e.DeathPoints).HasColumnName("deathPoints");
            entity.Property(e => e.Deaths).HasColumnName("deaths");
            entity.Property(e => e.Experience).HasColumnName("experience");
            entity.Property(e => e.ExperienceTotal).HasColumnName("experienceTotal");
            entity.Property(e => e.KillPoints).HasColumnName("killPoints");
            entity.Property(e => e.Kills).HasColumnName("kills");
            entity.Property(e => e.PlaySeconds).HasColumnName("playSeconds");
            entity.Property(e => e.VehicleDeaths).HasColumnName("vehicleDeaths");
            entity.Property(e => e.VehicleKills).HasColumnName("vehicleKills");
            entity.Property(e => e.ZoneId).HasColumnName("zone");
            entity.Property(e => e.Zonestat1).HasColumnName("zonestat1");
            entity.Property(e => e.Zonestat10).HasColumnName("zonestat10");
            entity.Property(e => e.Zonestat11).HasColumnName("zonestat11");
            entity.Property(e => e.Zonestat12).HasColumnName("zonestat12");
            entity.Property(e => e.Zonestat2).HasColumnName("zonestat2");
            entity.Property(e => e.Zonestat3).HasColumnName("zonestat3");
            entity.Property(e => e.Zonestat4).HasColumnName("zonestat4");
            entity.Property(e => e.Zonestat5).HasColumnName("zonestat5");
            entity.Property(e => e.Zonestat6).HasColumnName("zonestat6");
            entity.Property(e => e.Zonestat7).HasColumnName("zonestat7");
            entity.Property(e => e.Zonestat8).HasColumnName("zonestat8");
            entity.Property(e => e.Zonestat9).HasColumnName("zonestat9");

            entity.HasOne(d => d.ZoneNavigation).WithMany(p => p.Stats)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("zone_stats");
        });

        modelBuilder.Entity<StatsDaily>(entity =>
        {
            entity.ToTable("statsDaily");

            entity.Property(e => e.StatsDailyId).HasColumnName("id");
            entity.Property(e => e.AssistPoints).HasColumnName("assistPoints");
            entity.Property(e => e.BonusPoints).HasColumnName("bonusPoints");
            entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.DeathPoints).HasColumnName("deathPoints");
            entity.Property(e => e.Deaths).HasColumnName("deaths");
            entity.Property(e => e.Experience).HasColumnName("experience");
            entity.Property(e => e.ExperienceTotal).HasColumnName("experienceTotal");
            entity.Property(e => e.KillPoints).HasColumnName("killPoints");
            entity.Property(e => e.Kills).HasColumnName("kills");
            entity.Property(e => e.PlaySeconds).HasColumnName("playSeconds");
            entity.Property(e => e.PlayerId).HasColumnName("player");
            entity.Property(e => e.VehicleDeaths).HasColumnName("vehicleDeaths");
            entity.Property(e => e.VehicleKills).HasColumnName("vehicleKills");
            entity.Property(e => e.ZoneId).HasColumnName("zone");
            entity.Property(e => e.Zonestat1).HasColumnName("zonestat1");
            entity.Property(e => e.Zonestat10).HasColumnName("zonestat10");
            entity.Property(e => e.Zonestat11).HasColumnName("zonestat11");
            entity.Property(e => e.Zonestat12).HasColumnName("zonestat12");
            entity.Property(e => e.Zonestat2).HasColumnName("zonestat2");
            entity.Property(e => e.Zonestat3).HasColumnName("zonestat3");
            entity.Property(e => e.Zonestat4).HasColumnName("zonestat4");
            entity.Property(e => e.Zonestat5).HasColumnName("zonestat5");
            entity.Property(e => e.Zonestat6).HasColumnName("zonestat6");
            entity.Property(e => e.Zonestat7).HasColumnName("zonestat7");
            entity.Property(e => e.Zonestat8).HasColumnName("zonestat8");
            entity.Property(e => e.Zonestat9).HasColumnName("zonestat9");

            entity.HasOne(d => d.PlayerNavigation).WithMany(p => p.StatsDailies)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StatsDailyPlayer");

            entity.HasOne(d => d.ZoneNavigation).WithMany(p => p.StatsDailies)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("zone_statsDaily");
        });

        modelBuilder.Entity<StatsMonthly>(entity =>
        {
            entity.ToTable("statsMonthly");

            entity.Property(e => e.StatsMonthlyId).HasColumnName("id");
            entity.Property(e => e.AssistPoints).HasColumnName("assistPoints");
            entity.Property(e => e.BonusPoints).HasColumnName("bonusPoints");
            entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.DeathPoints).HasColumnName("deathPoints");
            entity.Property(e => e.Deaths).HasColumnName("deaths");
            entity.Property(e => e.Experience).HasColumnName("experience");
            entity.Property(e => e.ExperienceTotal).HasColumnName("experienceTotal");
            entity.Property(e => e.KillPoints).HasColumnName("killPoints");
            entity.Property(e => e.Kills).HasColumnName("kills");
            entity.Property(e => e.PlaySeconds).HasColumnName("playSeconds");
            entity.Property(e => e.PlayerId).HasColumnName("player");
            entity.Property(e => e.VehicleDeaths).HasColumnName("vehicleDeaths");
            entity.Property(e => e.VehicleKills).HasColumnName("vehicleKills");
            entity.Property(e => e.ZoneId).HasColumnName("zone");
            entity.Property(e => e.Zonestat1).HasColumnName("zonestat1");
            entity.Property(e => e.Zonestat10).HasColumnName("zonestat10");
            entity.Property(e => e.Zonestat11).HasColumnName("zonestat11");
            entity.Property(e => e.Zonestat12).HasColumnName("zonestat12");
            entity.Property(e => e.Zonestat2).HasColumnName("zonestat2");
            entity.Property(e => e.Zonestat3).HasColumnName("zonestat3");
            entity.Property(e => e.Zonestat4).HasColumnName("zonestat4");
            entity.Property(e => e.Zonestat5).HasColumnName("zonestat5");
            entity.Property(e => e.Zonestat6).HasColumnName("zonestat6");
            entity.Property(e => e.Zonestat7).HasColumnName("zonestat7");
            entity.Property(e => e.Zonestat8).HasColumnName("zonestat8");
            entity.Property(e => e.Zonestat9).HasColumnName("zonestat9");

            entity.HasOne(d => d.PlayerNavigation).WithMany(p => p.StatsMonthlies)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StatsMonthlyPlayer");

            entity.HasOne(d => d.ZoneNavigation).WithMany(p => p.StatsMonthlies)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("zone_statsMonthly");
        });

        modelBuilder.Entity<StatsWeekly>(entity =>
        {
            entity.ToTable("statsWeekly");

            entity.Property(e => e.StatsWeeklyId).HasColumnName("id");
            entity.Property(e => e.AssistPoints).HasColumnName("assistPoints");
            entity.Property(e => e.BonusPoints).HasColumnName("bonusPoints");
            entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.DeathPoints).HasColumnName("deathPoints");
            entity.Property(e => e.Deaths).HasColumnName("deaths");
            entity.Property(e => e.Experience).HasColumnName("experience");
            entity.Property(e => e.ExperienceTotal).HasColumnName("experienceTotal");
            entity.Property(e => e.KillPoints).HasColumnName("killPoints");
            entity.Property(e => e.Kills).HasColumnName("kills");
            entity.Property(e => e.PlaySeconds).HasColumnName("playSeconds");
            entity.Property(e => e.PlayerId).HasColumnName("player");
            entity.Property(e => e.VehicleDeaths).HasColumnName("vehicleDeaths");
            entity.Property(e => e.VehicleKills).HasColumnName("vehicleKills");
            entity.Property(e => e.ZoneId).HasColumnName("zone");
            entity.Property(e => e.Zonestat1).HasColumnName("zonestat1");
            entity.Property(e => e.Zonestat10).HasColumnName("zonestat10");
            entity.Property(e => e.Zonestat11).HasColumnName("zonestat11");
            entity.Property(e => e.Zonestat12).HasColumnName("zonestat12");
            entity.Property(e => e.Zonestat2).HasColumnName("zonestat2");
            entity.Property(e => e.Zonestat3).HasColumnName("zonestat3");
            entity.Property(e => e.Zonestat4).HasColumnName("zonestat4");
            entity.Property(e => e.Zonestat5).HasColumnName("zonestat5");
            entity.Property(e => e.Zonestat6).HasColumnName("zonestat6");
            entity.Property(e => e.Zonestat7).HasColumnName("zonestat7");
            entity.Property(e => e.Zonestat8).HasColumnName("zonestat8");
            entity.Property(e => e.Zonestat9).HasColumnName("zonestat9");

            entity.HasOne(d => d.PlayerNavigation).WithMany(p => p.StatsWeeklies)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StatsWeeklyPlayer");

            entity.HasOne(d => d.ZoneNavigation).WithMany(p => p.StatsWeeklies)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("zone_statsWeekly");
        });

        modelBuilder.Entity<StatsYearly>(entity =>
        {
            entity.ToTable("statsYearly");

            entity.Property(e => e.StatsYearlyId).HasColumnName("id");
            entity.Property(e => e.AssistPoints).HasColumnName("assistPoints");
            entity.Property(e => e.BonusPoints).HasColumnName("bonusPoints");
            entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.DeathPoints).HasColumnName("deathPoints");
            entity.Property(e => e.Deaths).HasColumnName("deaths");
            entity.Property(e => e.Experience).HasColumnName("experience");
            entity.Property(e => e.ExperienceTotal).HasColumnName("experienceTotal");
            entity.Property(e => e.KillPoints).HasColumnName("killPoints");
            entity.Property(e => e.Kills).HasColumnName("kills");
            entity.Property(e => e.PlaySeconds).HasColumnName("playSeconds");
            entity.Property(e => e.PlayerId).HasColumnName("player");
            entity.Property(e => e.VehicleDeaths).HasColumnName("vehicleDeaths");
            entity.Property(e => e.VehicleKills).HasColumnName("vehicleKills");
            entity.Property(e => e.ZoneId).HasColumnName("zone");
            entity.Property(e => e.Zonestat1).HasColumnName("zonestat1");
            entity.Property(e => e.Zonestat10).HasColumnName("zonestat10");
            entity.Property(e => e.Zonestat11).HasColumnName("zonestat11");
            entity.Property(e => e.Zonestat12).HasColumnName("zonestat12");
            entity.Property(e => e.Zonestat2).HasColumnName("zonestat2");
            entity.Property(e => e.Zonestat3).HasColumnName("zonestat3");
            entity.Property(e => e.Zonestat4).HasColumnName("zonestat4");
            entity.Property(e => e.Zonestat5).HasColumnName("zonestat5");
            entity.Property(e => e.Zonestat6).HasColumnName("zonestat6");
            entity.Property(e => e.Zonestat7).HasColumnName("zonestat7");
            entity.Property(e => e.Zonestat8).HasColumnName("zonestat8");
            entity.Property(e => e.Zonestat9).HasColumnName("zonestat9");

            entity.HasOne(d => d.PlayerNavigation).WithMany(p => p.StatsYearlies)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StatsYearlyPlayer");

            entity.HasOne(d => d.ZoneNavigation).WithMany(p => p.StatsYearlies)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("zone_statsYearly");
        });

        modelBuilder.Entity<Zone>(entity =>
        {
            entity.HasKey(e => e.ZoneId).HasName("PK_dbo.zone");

            entity.ToTable("zone");

            entity.Property(e => e.ZoneId).HasColumnName("id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Advanced).HasColumnName("advanced");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Ip)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ip");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Notice)
                .HasColumnType("text")
                .HasColumnName("notice");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.Port).HasColumnName("port");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
