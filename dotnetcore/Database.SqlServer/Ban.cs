using System;
using System.Collections.Generic;

namespace Database.SqlServer;

public partial class Ban
{
    public long BanId { get; set; }

    public short Type { get; set; }

    public long? AccountId { get; set; }

    public string? IpAddress { get; set; }

    public long? Uid1 { get; set; }

    public long? Uid2 { get; set; }

    public long? Uid3 { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Expires { get; set; }

    public long? ZoneId { get; set; }

    public string? Reason { get; set; }

    public string? Name { get; set; }
}
