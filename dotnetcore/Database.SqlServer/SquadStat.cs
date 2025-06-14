using System;
using System.Collections.Generic;

namespace Database.SqlServer;

public partial class SquadStat
{
    public long SquadStatId { get; set; }

    public int Kills { get; set; }

    public int Deaths { get; set; }

    public int Points { get; set; }

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int Rating { get; set; }

    public int? Season { get; set; }

    public long? SquadId { get; set; }
}
