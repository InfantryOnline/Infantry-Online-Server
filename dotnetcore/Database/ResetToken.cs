using System;
using System.Collections.Generic;

namespace Database;

public partial class ResetToken
{
    public long Id { get; set; }

    public long Account { get; set; }

    public string Name { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime ExpireDate { get; set; }

    public bool TokenUsed { get; set; }

    public virtual Account AccountNavigation { get; set; } = null!;
}
