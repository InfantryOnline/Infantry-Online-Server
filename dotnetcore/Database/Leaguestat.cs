using System;
using System.Collections.Generic;

namespace Database;

public partial class LeagueStat
{
    public long Id { get; set; }

    public long Zone { get; set; }

    public int Kills { get; set; }

    public int Deaths { get; set; }

    public int KillPoints { get; set; }

    public int DeathPoints { get; set; }

    public int AssistPoints { get; set; }

    public int BonusPoints { get; set; }

    public int PlaySeconds { get; set; }

    public long Match { get; set; }

    public long Player { get; set; }
}
