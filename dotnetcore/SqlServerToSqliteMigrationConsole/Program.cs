using AutoMapper;
using Database;
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
                cfg.CreateMap<Database.Account, Database.Account>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.Alias, Database.Alias>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.Player, Database.Player>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.Zone, Database.Zone>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.Squad, Database.Squad>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.Ban, Database.Ban>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.Helpcall, Database.Helpcall>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.Stat, Database.Stat>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.StatsDaily, Database.StatsDaily>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.StatsWeekly, Database.StatsWeekly>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.StatsMonthly, Database.StatsMonthly>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.StatsYearly, Database.StatsYearly>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.ResetToken, Database.ResetToken>()
                    .IgnoreAllVirtual();

                cfg.CreateMap<Database.History, Database.History>()
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

            List<Database.Account> oldAccounts;
            List<Database.Alias> oldAliases;
            List<Database.Player> oldPlayers;
            List<Database.Ban> oldBans;
            List<Database.History> oldHistory;
            List<Database.ResetToken> oldResetTokens;
            List<Database.Helpcall> oldHelpCalls;
            List<Database.Squad> oldSquads;
            List<Database.Stat> oldStats;
            List<Database.StatsDaily> oldStatsDaily;
            List<Database.StatsWeekly> oldStatsWeekly;
            List<Database.StatsMonthly> oldStatsMonthly;
            List<Database.StatsYearly> oldStatsYearly;
            List<Database.Zone> oldZones;

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

            var accMap = new Dictionary<long, Database.Account>();

            using (var ctx = new SqliteDbContext())
            {
                foreach (var acc in oldAccounts)
                {
                    var newAcc = mapper.Map<Database.Account>(acc);

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

            var aliasMap = new Dictionary<long, Database.Alias>();

            using (var ctx = new SqliteDbContext())
            {
                if (ctx.Aliases.Count() > 0)
                {
                    throw new Exception("Database isn't empty. Please provide a clean database");
                }

                foreach (var oldAl in oldAliases)
                {
                    var newAl = mapper.Map<Database.Alias>(oldAl);

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

            var zoneMap = new Dictionary<long, Database.Zone>();

            using (var ctx = new SqliteDbContext())
            {
                if (ctx.Zones.Count() > 0)
                {
                    throw new Exception("Database isn't empty. Please provide a clean database");
                }

                foreach (var oldZ in oldZones)
                {
                    var newZ = mapper.Map<Database.Zone>(oldZ);

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

            var statsMap = new Dictionary<long, Database.Stat>();

            using (var ctx = new SqliteDbContext())
            {
                if (ctx.Stats.Count() > 0)
                {
                    throw new Exception("Database isn't empty. Please provide a clean database");
                }

                foreach (var oldS in oldStats)
                {
                    var newS = mapper.Map<Database.Stat>(oldS);

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

            var playerMap = new Dictionary<long, Database.Player>();

            using (var ctx = new SqliteDbContext())
            {
                if (ctx.Players.Count() > 0)
                {
                    throw new Exception("Database isn't empty. Please provide a clean database");
                }

                foreach (var oldP in oldPlayers)
                {
                    var newP = mapper.Map<Database.Player>(oldP);

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
                    var newS = mapper.Map<Database.StatsDaily>(oldS);

                    newS.StatsDailyId = 0;
                    newS.ZoneId = zoneMap[oldS.ZoneId].ZoneId;
                    newS.PlayerId = playerMap[oldS.PlayerId].PlayerId;

                    ctx.StatsDailies.Add(newS);
                }

                oldStatsDaily.Clear();

                foreach (var oldS in oldStatsWeekly)
                {
                    var newS = mapper.Map<Database.StatsWeekly>(oldS);

                    newS.StatsWeeklyId = 0;
                    newS.ZoneId = zoneMap[oldS.ZoneId].ZoneId;
                    newS.PlayerId = playerMap[oldS.PlayerId].PlayerId;

                    ctx.StatsWeeklies.Add(newS);
                }

                oldStatsWeekly.Clear();

                foreach (var oldS in oldStatsMonthly)
                {
                    var newS = mapper.Map<Database.StatsMonthly>(oldS);

                    newS.StatsMonthlyId = 0;
                    newS.ZoneId = zoneMap[oldS.ZoneId].ZoneId;
                    newS.PlayerId = playerMap[oldS.PlayerId].PlayerId;

                    ctx.StatsMonthlies.Add(newS);
                }

                oldStatsMonthly.Clear();

                foreach (var oldS in oldStatsYearly)
                {
                    var newS = mapper.Map<Database.StatsYearly>(oldS);

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

            var squadMap = new Dictionary<long, Database.Squad>();

            using (var ctx = new SqliteDbContext())
            {
                if (ctx.Squads.Count() > 0)
                {
                    throw new Exception("Database isn't empty. Please provide a clean database");
                }

                foreach (var oldS in oldSquads)
                {
                    var newS = mapper.Map<Database.Squad>(oldS);

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
                    var nb = mapper.Map<Database.Ban>(ob);

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
                    var nh = mapper.Map<Database.History>(oh);

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
                    var nhc = mapper.Map<Database.Helpcall>(ohc);

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
                    var nrt = mapper.Map<Database.ResetToken>(ort);

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
