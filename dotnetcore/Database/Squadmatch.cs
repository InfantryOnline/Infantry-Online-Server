using System;
using System.Collections.Generic;

namespace Database;

public partial class SquadMatch
{
    public long Id { get; set; }

    public long Zone { get; set; }

    public DateTime? DateBegin { get; set; }

    public DateTime? DateEnd { get; set; }

    public long Squad1 { get; set; }

    public long Squad2 { get; set; }

    public long? Winner { get; set; }

    public long? Loser { get; set; }

    public int Season { get; set; }
}
