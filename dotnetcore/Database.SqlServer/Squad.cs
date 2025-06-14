using System;
using System.Collections.Generic;

namespace Database.SqlServer;

public partial class Squad
{
    public long SquadId { get; set; }

    public string Name { get; set; } = null!;

    public string Password { get; set; } = null!;

    public DateTime DateCreated { get; set; }

    public long OwnerPlayerId { get; set; }

    public long ZoneId { get; set; }

    public long SquadStatsId { get; set; }

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();
}
