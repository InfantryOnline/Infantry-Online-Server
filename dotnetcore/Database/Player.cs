using System;
using System.Collections.Generic;

namespace Database;

public partial class Player
{
    public long Id { get; set; }

    public long Alias { get; set; }

    public long Zone { get; set; }

    public long? Squad { get; set; }

    public long Stats { get; set; }

    public short Permission { get; set; }

    public DateTime LastAccess { get; set; }

    public byte[]? Inventory { get; set; }

    public byte[]? Skills { get; set; }

    public byte[]? Banner { get; set; }

    public virtual Alias AliasNavigation { get; set; } = null!;

    public virtual Squad? SquadNavigation { get; set; }

    public virtual ICollection<StatsDaily> StatsDailies { get; set; } = new List<StatsDaily>();

    public virtual ICollection<StatsMonthly> StatsMonthlies { get; set; } = new List<StatsMonthly>();

    public virtual Stat StatsNavigation { get; set; } = null!;

    public virtual ICollection<StatsWeekly> StatsWeeklies { get; set; } = new List<StatsWeekly>();

    public virtual ICollection<StatsYearly> StatsYearlies { get; set; } = new List<StatsYearly>();

    public virtual Zone ZoneNavigation { get; set; } = null!;
}
