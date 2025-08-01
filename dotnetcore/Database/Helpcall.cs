﻿using System;
using System.Collections.Generic;

namespace Database;

public partial class Helpcall
{
    public long HelpCallId { get; set; }

    public string Sender { get; set; } = null!;

    public string Arena { get; set; } = null!;

    public string Zone { get; set; } = null!;

    public DateTime Date { get; set; }

    public string? Reason { get; set; }
}
