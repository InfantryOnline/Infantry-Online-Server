using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Ticket = table.Column<string>(type: "TEXT", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastAccess = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Permission = table.Column<int>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ForumId = table.Column<long>(type: "INTEGER", nullable: true),
                    SilencedAtMillisecondsUnix = table.Column<long>(type: "INTEGER", nullable: false),
                    SilencedDuration = table.Column<long>(type: "INTEGER", nullable: false),
                    BannerMode = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "Bans",
                columns: table => new
                {
                    BanId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<short>(type: "INTEGER", nullable: false),
                    AccountId = table.Column<long>(type: "INTEGER", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Uid1 = table.Column<long>(type: "INTEGER", nullable: true),
                    Uid2 = table.Column<long>(type: "INTEGER", nullable: true),
                    Uid3 = table.Column<long>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Expires = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ZoneId = table.Column<long>(type: "INTEGER", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bans", x => x.BanId);
                });

            migrationBuilder.CreateTable(
                name: "Helpcalls",
                columns: table => new
                {
                    HelpCallId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sender = table.Column<string>(type: "TEXT", nullable: false),
                    Arena = table.Column<string>(type: "TEXT", nullable: false),
                    Zone = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Helpcalls", x => x.HelpCallId);
                });

            migrationBuilder.CreateTable(
                name: "Histories",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sender = table.Column<string>(type: "TEXT", nullable: false),
                    Recipient = table.Column<string>(type: "TEXT", nullable: false),
                    Zone = table.Column<string>(type: "TEXT", nullable: false),
                    Arena = table.Column<string>(type: "TEXT", nullable: false),
                    Command = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Histories", x => x.HistoryId);
                });

            migrationBuilder.CreateTable(
                name: "Squads",
                columns: table => new
                {
                    SquadId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OwnerPlayerId = table.Column<long>(type: "INTEGER", nullable: false),
                    ZoneId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Squads", x => x.SquadId);
                });

            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    ZoneId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Notice = table.Column<string>(type: "TEXT", nullable: false),
                    Active = table.Column<short>(type: "INTEGER", nullable: false),
                    Ip = table.Column<string>(type: "TEXT", nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: true),
                    Advanced = table.Column<short>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.ZoneId);
                });

            migrationBuilder.CreateTable(
                name: "Aliases",
                columns: table => new
                {
                    AliasId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Creation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: false),
                    LastAccess = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimePlayed = table.Column<long>(type: "INTEGER", nullable: false),
                    Stealth = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aliases", x => x.AliasId);
                    table.ForeignKey(
                        name: "FK_Aliases_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResetTokens",
                columns: table => new
                {
                    ResetTokenId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    ExpireDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TokenUsed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResetTokens", x => x.ResetTokenId);
                    table.ForeignKey(
                        name: "FK_ResetTokens_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stats",
                columns: table => new
                {
                    StatId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ZoneId = table.Column<long>(type: "INTEGER", nullable: false),
                    Cash = table.Column<int>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    ExperienceTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Kills = table.Column<int>(type: "INTEGER", nullable: false),
                    Deaths = table.Column<int>(type: "INTEGER", nullable: false),
                    KillPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    DeathPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    AssistPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    BonusPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleKills = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleDeaths = table.Column<int>(type: "INTEGER", nullable: false),
                    PlaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat3 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat4 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat5 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat6 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat7 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat8 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat9 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat10 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat11 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat12 = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stats", x => x.StatId);
                    table.ForeignKey(
                        name: "FK_Stats_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AliasId = table.Column<long>(type: "INTEGER", nullable: false),
                    ZoneId = table.Column<long>(type: "INTEGER", nullable: false),
                    SquadId = table.Column<long>(type: "INTEGER", nullable: true),
                    StatId = table.Column<long>(type: "INTEGER", nullable: false),
                    Permission = table.Column<short>(type: "INTEGER", nullable: false),
                    LastAccess = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Inventory = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Skills = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Banner = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_Players_Aliases_AliasId",
                        column: x => x.AliasId,
                        principalTable: "Aliases",
                        principalColumn: "AliasId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Players_Squads_SquadId",
                        column: x => x.SquadId,
                        principalTable: "Squads",
                        principalColumn: "SquadId");
                    table.ForeignKey(
                        name: "FK_Players_Stats_StatId",
                        column: x => x.StatId,
                        principalTable: "Stats",
                        principalColumn: "StatId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Players_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatsDailies",
                columns: table => new
                {
                    StatsDailyId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ZoneId = table.Column<long>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    ExperienceTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Kills = table.Column<int>(type: "INTEGER", nullable: false),
                    Deaths = table.Column<int>(type: "INTEGER", nullable: false),
                    KillPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    DeathPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    AssistPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    BonusPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleKills = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleDeaths = table.Column<int>(type: "INTEGER", nullable: false),
                    PlaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false),
                    Zonestat1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat3 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat4 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat5 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat6 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat7 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat8 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat9 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat10 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat11 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat12 = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsDailies", x => x.StatsDailyId);
                    table.ForeignKey(
                        name: "FK_StatsDailies_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StatsDailies_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatsMonthlies",
                columns: table => new
                {
                    StatsMonthlyId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ZoneId = table.Column<long>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    ExperienceTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Kills = table.Column<int>(type: "INTEGER", nullable: false),
                    Deaths = table.Column<int>(type: "INTEGER", nullable: false),
                    KillPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    DeathPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    AssistPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    BonusPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleKills = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleDeaths = table.Column<int>(type: "INTEGER", nullable: false),
                    PlaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false),
                    Zonestat1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat3 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat4 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat5 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat6 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat7 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat8 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat9 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat10 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat11 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat12 = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsMonthlies", x => x.StatsMonthlyId);
                    table.ForeignKey(
                        name: "FK_StatsMonthlies_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StatsMonthlies_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatsWeeklies",
                columns: table => new
                {
                    StatsWeeklyId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ZoneId = table.Column<long>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    ExperienceTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Kills = table.Column<int>(type: "INTEGER", nullable: false),
                    Deaths = table.Column<int>(type: "INTEGER", nullable: false),
                    KillPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    DeathPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    BonusPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleKills = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleDeaths = table.Column<int>(type: "INTEGER", nullable: false),
                    PlaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AssistPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false),
                    Zonestat1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat3 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat4 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat5 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat6 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat7 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat8 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat9 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat10 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat11 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat12 = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsWeeklies", x => x.StatsWeeklyId);
                    table.ForeignKey(
                        name: "FK_StatsWeeklies_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StatsWeeklies_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatsYearlies",
                columns: table => new
                {
                    StatsYearlyId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ZoneId = table.Column<long>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    ExperienceTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Kills = table.Column<int>(type: "INTEGER", nullable: false),
                    Deaths = table.Column<int>(type: "INTEGER", nullable: false),
                    KillPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    DeathPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    AssistPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    BonusPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleKills = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleDeaths = table.Column<int>(type: "INTEGER", nullable: false),
                    PlaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false),
                    Zonestat1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat3 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat4 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat5 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat6 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat7 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat8 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat9 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat10 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat11 = table.Column<int>(type: "INTEGER", nullable: false),
                    Zonestat12 = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsYearlies", x => x.StatsYearlyId);
                    table.ForeignKey(
                        name: "FK_StatsYearlies_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StatsYearlies_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aliases_AccountId",
                table: "Aliases",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Aliases_Name",
                table: "Aliases",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_AliasId_ZoneId",
                table: "Players",
                columns: new[] { "AliasId", "ZoneId" });

            migrationBuilder.CreateIndex(
                name: "IX_Players_SquadId",
                table: "Players",
                column: "SquadId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_StatId",
                table: "Players",
                column: "StatId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_ZoneId",
                table: "Players",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ResetTokens_AccountId",
                table: "ResetTokens",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Stats_ZoneId",
                table: "Stats",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_StatsDailies_PlayerId_Date",
                table: "StatsDailies",
                columns: new[] { "PlayerId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatsDailies_ZoneId",
                table: "StatsDailies",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_StatsMonthlies_PlayerId_Date",
                table: "StatsMonthlies",
                columns: new[] { "PlayerId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatsMonthlies_ZoneId",
                table: "StatsMonthlies",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_StatsWeeklies_PlayerId_Date",
                table: "StatsWeeklies",
                columns: new[] { "PlayerId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatsWeeklies_ZoneId",
                table: "StatsWeeklies",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_StatsYearlies_PlayerId_Date",
                table: "StatsYearlies",
                columns: new[] { "PlayerId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatsYearlies_ZoneId",
                table: "StatsYearlies",
                column: "ZoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bans");

            migrationBuilder.DropTable(
                name: "Helpcalls");

            migrationBuilder.DropTable(
                name: "Histories");

            migrationBuilder.DropTable(
                name: "ResetTokens");

            migrationBuilder.DropTable(
                name: "StatsDailies");

            migrationBuilder.DropTable(
                name: "StatsMonthlies");

            migrationBuilder.DropTable(
                name: "StatsWeeklies");

            migrationBuilder.DropTable(
                name: "StatsYearlies");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Aliases");

            migrationBuilder.DropTable(
                name: "Squads");

            migrationBuilder.DropTable(
                name: "Stats");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Zones");
        }
    }
}
