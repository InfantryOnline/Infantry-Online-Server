using System;
using System.Collections.Generic;

namespace Database;

public partial class Zmod
{
    public long Id { get; set; }

    public long Account { get; set; }

    public long Zone { get; set; }

    public int Level { get; set; }

    public virtual Account AccountNavigation { get; set; } = null!;

    public virtual Zone ZoneNavigation { get; set; } = null!;
}
