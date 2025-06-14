using System;
using System.Collections.Generic;

namespace Database.SqlServer;

public partial class History
{
    public long HistoryId { get; set; }

    public string Sender { get; set; } = null!;

    public string Recipient { get; set; } = null!;

    public string Zone { get; set; } = null!;

    public string Arena { get; set; } = null!;

    public string Command { get; set; } = null!;

    public DateTime Date { get; set; }
}
