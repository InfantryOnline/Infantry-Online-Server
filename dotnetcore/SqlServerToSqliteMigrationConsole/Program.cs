using AutoMapper;
using Database.Sqlite;
using Database.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace SqlServerToSqliteMigrationConsole
{
    public static class IgnoreVirtualExtensions
    {
        public static IMappingExpression<TSource, TDestination>
               IgnoreAllVirtual<TSource, TDestination>(
                   this IMappingExpression<TSource, TDestination> expression)
        {
            var desType = typeof(TDestination);
            foreach (var property in desType.GetProperties().Where(p =>
                                     p.GetGetMethod().IsVirtual))
            {
                expression.ForMember(property.Name, opt => opt.Ignore());
            }

            return expression;
        }
    }

    internal class Program
    {
        static IMapper InitializeAutoMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Database.SqlServer.Account, Database.Sqlite.Account>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.Alias, Database.Sqlite.Alias>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.Player, Database.Sqlite.Player>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.Zone, Database.Sqlite.Zone>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.Squad, Database.Sqlite.Squad>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.Ban, Database.Sqlite.Ban>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.Helpcall, Database.Sqlite.Helpcall>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.Stat, Database.Sqlite.Stat>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.StatsDaily, Database.Sqlite.StatsDaily>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.StatsWeekly, Database.Sqlite.StatsWeekly>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.StatsMonthly, Database.Sqlite.StatsMonthly>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.StatsYearly, Database.Sqlite.StatsYearly>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.ResetToken, Database.Sqlite.ResetToken>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.SqlServer.History, Database.Sqlite.History>()
                    .IgnoreAllVirtual();
            });

            return config.CreateMapper();
        }

        static void Main(string[] args)
        {
            var mapper = InitializeAutoMapper();

            using (var ctx = new SqliteDbContext())
            {
                // ctx.Database.EnsureCreated();
                ctx.Database.Migrate();

                if (ctx.Accounts.Count() > 0)
                {
                    throw new Exception("Sqlite isn't empty. Please provide a clean database");
                }
            }

            Console.WriteLine("Starting migration process. This will take some time...");

            List<Database.SqlServer.Account> oldAccounts;
            List<Database.SqlServer.Alias> oldAliases;
            List<Database.SqlServer.Player> oldPlayers;
            List<Database.SqlServer.Ban> oldBans;
            List<Database.SqlServer.History> oldHistory;
            List<Database.SqlServer.ResetToken> oldResetTokens;
            List<Database.SqlServer.Helpcall> oldHelpCalls;
            List<Database.SqlServer.Squad> oldSquads;
            List<Database.SqlServer.Stat> oldStats;
            List<Database.SqlServer.StatsDaily> oldStatsDaily;
            List<Database.SqlServer.StatsWeekly> oldStatsWeekly;
            List<Database.SqlServer.StatsMonthly> oldStatsMonthly;
            List<Database.SqlServer.StatsYearly> oldStatsYearly;
            List<Database.SqlServer.Zone> oldZones;

            var options = new DbContextOptionsBuilder<SqlServerDbContext>()
            .UseSqlServer("Data Source=JOVAN\\SQLEXPRESS01;Database=Data;Trusted_Connection=True;TrustServerCertificate=true")
                .Options;

            var oldDbCtxFactory = new PooledDbContextFactory<SqlServerDbContext>(options);

            using (var ctx = oldDbCtxFactory.CreateDbContext())
            {
                oldAccounts = ctx.Accounts.ToList();
                oldAliases = ctx.Aliases.ToList();
                oldPlayers = ctx.Players.ToList();
                oldBans = ctx.Bans.ToList();
                oldHistory = ctx.Histories.Where(h => h.Date >= new DateTime(2024, 1, 1)).ToList();
                oldSquads = ctx.Squads.ToList();
                oldStats = ctx.Stats.ToList();
                oldZones = ctx.Zones.ToList();
                oldResetTokens = ctx.ResetTokens.ToList();
                oldHelpCalls = ctx.Helpcalls.ToList();
                oldStatsDaily = ctx.StatsDailies.ToList();
                oldStatsWeekly = ctx.StatsWeeklies.ToList();
                oldStatsMonthly = ctx.StatsMonthlies.ToList();
                oldStatsYearly = ctx.StatsYearlies.ToList();
            }

            Console.WriteLine("Loaded old database records.");
            Console.WriteLine("Seeding new database...");

            #region Create Accounts

            Console.WriteLine("1. Creating accounts...");

            var accMap = new Dictionary<long, Database.Sqlite.Account>();

            using (var ctx = new SqliteDbContext())
            {
                foreach (var acc in oldAccounts)
                {
                    var newAcc = mapper.Map<Database.Sqlite.Account>(acc);

                    newAcc.AccountId = 0;

                    ctx.Accounts.Add(newAcc);

                    accMap.Add(acc.AccountId, newAcc);
                }

                ctx.SaveChanges();
            }

            oldAccounts.Clear();

            #endregion

            #region Create Aliases
            Console.WriteLine("2. Creating aliases...");

            var aliasMap = new Dictionary<long, Database.Sqlite.Alias>();

            using (var ctx = new SqliteDbContext())
            {
                if (ctx.Aliases.Count() > 0)
                {
                    throw new Exception("Database isn't empty. Please provide a clean database");
                }

                foreach (var oldAl in oldAliases)
                {
                    var newAl = mapper.Map<Database.Sqlite.Alias>(oldAl);

                    newAl.AliasId = 0;

                    // Map accounts over to aliases.
                    if (accMap.ContainsKey(oldAl.AccountId))
                    {
                        newAl.AccountId = accMap[oldAl.AccountId].AccountId;
                    }

                    aliasMap.Add(oldAl.AliasId, newAl);

                    ctx.Aliases.Add(newAl);
                }

                ctx.SaveChanges();
            }

            oldAliases.Clear();

            #endregion

            #region Create Zones

            Console.WriteLine("3. Creating zones...");

            var zoneMap = new Dictionary<long, Database.Sqlite.Zone>();

            using (var ctx = new SqliteDbContext())
            {
                if (ctx.Zones.Count() > 0)
                {
                    throw new Exception("Database isn't empty. Please provide a clean database");
                }

                foreach (var oldZ in oldZones)
                {
                    var newZ = mapper.Map<Database.Sqlite.Zone>(oldZ);

                    newZ.ZoneId = 0;

                    zoneMap.Add(oldZ.ZoneId, newZ);

                    ctx.Zones.Add(newZ);
                }

                ctx.SaveChanges();
            }

            oldZones.Clear();

            #endregion

            #region Create Stats

            Console.WriteLine("4. Creating stats...");

            var statsMap = new Dictionary<long, Database.Sqlite.Stat>();

            using (var ctx = new SqliteDbContext())
            {
                if (ctx.Stats.Count() > 0)
                {
                    throw new Exception("Database isn't empty. Please provide a clean database");
                }

                foreach (var oldS in oldStats)
                {
                    var newS = mapper.Map<Database.Sqlite.Stat>(oldS);

                    newS.StatId = 0;
                    newS.ZoneId = zoneMap[oldS.ZoneId].ZoneId;

                    statsMap.Add(oldS.StatId, newS);

                    ctx.Stats.Add(newS);
                }

                ctx.SaveChanges();
            }

            oldStats.Clear();

            #endregion

            #region Create Players

            Console.WriteLine("5. Creating players...");

            var playerMap = new Dictionary<long, Database.Sqlite.Player>();

            using (var ctx = new SqliteDbContext())
            {
                if (ctx.Players.Count() > 0)
                {
                    throw new Exception("Database isn't empty. Please provide a clean database");
                }

                foreach (var oldP in oldPlayers)
                {
                    var newP = mapper.Map<Database.Sqlite.Player>(oldP);

                    newP.PlayerId = 0;
                    newP.AliasId = aliasMap[oldP.AliasId].AliasId;
                    newP.StatId = statsMap[oldP.StatId].StatId;
                    newP.ZoneId = zoneMap[oldP.ZoneId].ZoneId;

                    newP.SquadId = null; // Set as null; we will update it when we load squads in.

                    playerMap.Add(oldP.PlayerId, newP);

                    ctx.Players.Add(newP);
                }

                ctx.SaveChanges();
            }

            #endregion

            #region Create Historic Stats

            Console.WriteLine("6. Creating history stats...");

            using (var ctx = new SqliteDbContext())
            {
                foreach (var oldS in oldStatsDaily)
                {
                    var newS = mapper.Map<Database.Sqlite.StatsDaily>(oldS);

                    newS.StatsDailyId = 0;
                    newS.ZoneId = zoneMap[oldS.ZoneId].ZoneId;
                    newS.PlayerId = playerMap[oldS.PlayerId].PlayerId;

                    ctx.StatsDailies.Add(newS);
                }

                oldStatsDaily.Clear();

                foreach (var oldS in oldStatsWeekly)
                {
                    var newS = mapper.Map<Database.Sqlite.StatsWeekly>(oldS);

                    newS.StatsWeeklyId = 0;
                    newS.ZoneId = zoneMap[oldS.ZoneId].ZoneId;
                    newS.PlayerId = playerMap[oldS.PlayerId].PlayerId;

                    ctx.StatsWeeklies.Add(newS);
                }

                oldStatsWeekly.Clear();

                foreach (var oldS in oldStatsMonthly)
                {
                    var newS = mapper.Map<Database.Sqlite.StatsMonthly>(oldS);

                    newS.StatsMonthlyId = 0;
                    newS.ZoneId = zoneMap[oldS.ZoneId].ZoneId;
                    newS.PlayerId = playerMap[oldS.PlayerId].PlayerId;

                    ctx.StatsMonthlies.Add(newS);
                }

                oldStatsMonthly.Clear();

                foreach (var oldS in oldStatsYearly)
                {
                    var newS = mapper.Map<Database.Sqlite.StatsYearly>(oldS);

                    newS.StatsYearlyId = 0;
                    newS.ZoneId = zoneMap[oldS.ZoneId].ZoneId;
                    newS.PlayerId = playerMap[oldS.PlayerId].PlayerId;

                    ctx.StatsYearlies.Add(newS);
                }

                oldStatsYearly.Clear();

                ctx.SaveChanges();
            }

            #endregion

            #region Create Squads

            Console.WriteLine("7. Creating squads...");

            var squadMap = new Dictionary<long, Database.Sqlite.Squad>();

            using (var ctx = new SqliteDbContext())
            {
                if (ctx.Squads.Count() > 0)
                {
                    throw new Exception("Database isn't empty. Please provide a clean database");
                }

                foreach (var oldS in oldSquads)
                {
                    var newS = mapper.Map<Database.Sqlite.Squad>(oldS);

                    newS.SquadId = 0;
                    newS.ZoneId = zoneMap[oldS.ZoneId].ZoneId;

                    if (playerMap.ContainsKey(oldS.OwnerPlayerId))
                    {
                        newS.OwnerPlayerId = playerMap[oldS.OwnerPlayerId].PlayerId;
                    }

                    squadMap.Add(oldS.SquadId, newS);

                    ctx.Squads.Add(newS);
                }

                ctx.SaveChanges();
            }

            Console.WriteLine("7a. Saving Squads->Players...");

            using (var ctx = new SqliteDbContext())
            {
                foreach (var oldP in oldPlayers)
                {
                    var newP = ctx.Players.Find(playerMap[oldP.PlayerId].PlayerId);

                    if (oldP.SquadId.HasValue)
                    {
                        newP.SquadId = squadMap[oldP.SquadId.Value].SquadId;
                    }
                }

                ctx.SaveChanges();
            }

            oldSquads.Clear();
            oldPlayers.Clear();

            #endregion

            #region Create Bans

            Console.WriteLine("8. Creating bans...");

            using (var ctx = new SqliteDbContext())
            {
                foreach (var ob in oldBans)
                {
                    var nb = mapper.Map<Database.Sqlite.Ban>(ob);

                    nb.BanId = 0;
                    nb.ZoneId = ob.ZoneId.HasValue ? zoneMap[ob.ZoneId.Value].ZoneId : null;
                    nb.AccountId = ob.AccountId.HasValue ? accMap[ob.AccountId.Value].AccountId : null;

                    ctx.Bans.Add(nb);
                }

                ctx.SaveChanges();
            }

            oldBans.Clear();

            #endregion

            #region Create History

            Console.WriteLine("9. Creating history logs...");

            using (var ctx = new SqliteDbContext())
            {
                foreach (var oh in oldHistory)
                {
                    var nh = mapper.Map<Database.Sqlite.History>(oh);

                    nh.HistoryId = 0;

                    ctx.Histories.Add(nh);
                }

                ctx.SaveChanges();
            }

            oldHistory.Clear();

            #endregion

            #region Create Help Calls

            Console.WriteLine("10. Creating help calls...");

            using (var ctx = new SqliteDbContext())
            {
                foreach(var ohc in oldHelpCalls)
                {
                    var nhc = mapper.Map<Database.Sqlite.Helpcall>(ohc);

                    nhc.HelpCallId = 0;

                    ctx.Helpcalls.Add(nhc);
                }

                ctx.SaveChanges();
            }

            #endregion

            #region Create Reset Tokens

            Console.WriteLine("11. Creating reset tokens...");

            using (var ctx = new SqliteDbContext())
            {
                foreach(var ort in oldResetTokens)
                {
                    var nrt = mapper.Map<Database.Sqlite.ResetToken>(ort);

                    nrt.ResetTokenId = 0;
                    nrt.AccountId = accMap[ort.AccountId].AccountId;

                    ctx.ResetTokens.Add(nrt);
                }

                ctx.SaveChanges();
            }

            oldResetTokens.Clear();

            #endregion

            Console.WriteLine("Migration successfully completed.");
        }
    }
}
