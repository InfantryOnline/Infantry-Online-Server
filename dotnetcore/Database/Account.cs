using System;
using System.Collections.Generic;

namespace Database;

public partial class Account
{
    public long AccountId { get; set; }

    public string Name { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Ticket { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime LastAccess { get; set; }

    public int Permission { get; set; }

    public string Email { get; set; } = null!;

    public string? IpAddress { get; set; }

    public long? ForumId { get; set; }

    public long SilencedAtMillisecondsUnix { get; set; }

    public long SilencedDuration { get; set; }

    public byte BannerMode { get; set; }

    public virtual ICollection<Alias> Aliases { get; set; } = new List<Alias>();

    public virtual ICollection<ResetToken> ResetTokens { get; set; } = new List<ResetToken>();
}
