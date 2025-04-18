using System;
using System.Collections.Generic;

namespace Database;

public partial class Ban
{
    public long Id { get; set; }

    public short Type { get; set; }

    public long? Account { get; set; }

    public string? Ipaddress { get; set; }

    public long? Uid1 { get; set; }

    public long? Uid2 { get; set; }

    public long? Uid3 { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Expires { get; set; }

    public long? Zone { get; set; }

    public string? Reason { get; set; }

    public string? Name { get; set; }
}
