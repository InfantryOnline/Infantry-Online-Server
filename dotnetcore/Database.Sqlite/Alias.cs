using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Database.Sqlite;

[Index(nameof(Name), IsUnique = true)]
public partial class Alias
{
    public long AliasId { get; set; }

    public long AccountId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime Creation { get; set; }

    public string IpAddress { get; set; } = null!;

    public DateTime LastAccess { get; set; }

    public long TimePlayed { get; set; }

    public int Stealth { get; set; }

    public virtual Account AccountNavigation { get; set; } = null!;

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();
}
