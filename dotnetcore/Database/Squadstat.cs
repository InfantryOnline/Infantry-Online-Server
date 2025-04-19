using System;
using System.Collections.Generic;

namespace Database;

public partial class SquadStat
{
    public long Id { get; set; }

    public int Kills { get; set; }

    public int Deaths { get; set; }

    public int Points { get; set; }

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int Rating { get; set; }

    public int? Season { get; set; }

    public long? Squad { get; set; }
}
