using System;
using System.Collections.Generic;

namespace Database.Sqlite;

public partial class Stat
{
    public long StatId { get; set; }

    public long ZoneId { get; set; }

    public int Cash { get; set; }

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

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();

    public virtual Zone ZoneNavigation { get; set; } = null!;
}
