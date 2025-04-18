using System;
using System.Collections.Generic;

namespace Database;

public partial class Squad
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string Password { get; set; } = null!;

    public DateTime DateCreated { get; set; }

    public long Owner { get; set; }

    public long Zone { get; set; }

    public long Stats { get; set; }

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();
}
