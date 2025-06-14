using System;
using System.Collections.Generic;

namespace Database.SqlServer;

public partial class ResetToken
{
    public long ResetTokenId { get; set; }

    public long AccountId { get; set; }

    public string Name { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime ExpireDate { get; set; }

    public bool TokenUsed { get; set; }

    public virtual Account AccountNavigation { get; set; } = null!;
}
