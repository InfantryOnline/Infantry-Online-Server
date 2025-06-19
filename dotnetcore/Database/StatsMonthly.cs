using Microsoft.EntityFrameworkCore;

namespace Database;

[Index(nameof(PlayerId), nameof(Date), IsUnique = true)]
public partial class StatsMonthly
{
    public long StatsMonthlyId { get; set; }

    public long ZoneId { get; set; }

    public int Experience { get; set; }

    public int ExperienceTotal { get; set; }

    public int Kills { get; set; }

    public int Deaths { get; set; }

    public int KillPoints { get; set; }

    public int DeathPoints { get; set; }

    public int AssistPoints { get; set; }

    public int BonusPoints { get; set; }

    public int VehicleKills { get; set; }

    public int VehicleDeaths { get; set; }

    public int PlaySeconds { get; set; }

    public DateTime Date { get; set; }

    public long PlayerId { get; set; }

    public int Zonestat1 { get; set; }

    public int Zonestat2 { get; set; }

    public int Zonestat3 { get; set; }

    public int Zonestat4 { get; set; }

    public int Zonestat5 { get; set; }

    public int Zonestat6 { get; set; }

    public int Zonestat7 { get; set; }

    public int Zonestat8 { get; set; }

    public int Zonestat9 { get; set; }

    public int Zonestat10 { get; set; }

    public int Zonestat11 { get; set; }

    public int Zonestat12 { get; set; }

    public virtual Player PlayerNavigation { get; set; } = null!;

    public virtual Zone ZoneNavigation { get; set; } = null!;
}
