using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseServerNATS.Options
{
    public class ZoneServerOptions
    {
        public const string SectionName = "ZoneServer";

        [Required]
        public required long HeartbeatIntervalMs { get; set; } = 5000;
    }
}
