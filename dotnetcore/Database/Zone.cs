using System;
using System.Collections.Generic;

namespace Database;

public partial class Zone
{
    public long Id { get; set; }

    public string Password { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Notice { get; set; } = null!;

    public short Active { get; set; }

    public string? Ip { get; set; }

    public int? Port { get; set; }

    public short? Advanced { get; set; }

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();

    public virtual ICollection<Stat> Stats { get; set; } = new List<Stat>();

    public virtual ICollection<StatsDaily> StatsDailies { get; set; } = new List<StatsDaily>();

    public virtual ICollection<StatsMonthly> StatsMonthlies { get; set; } = new List<StatsMonthly>();

    public virtual ICollection<StatsWeekly> StatsWeeklies { get; set; } = new List<StatsWeekly>();

    public virtual ICollection<StatsYearly> StatsYearlies { get; set; } = new List<StatsYearly>();
}
