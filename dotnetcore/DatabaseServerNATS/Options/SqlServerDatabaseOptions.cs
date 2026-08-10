using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseServerNATS.Options
{
    public class SqlServerDatabaseOptions
    {
        public const string SectionName = "SqlServerDatabase";

        [Required]
        public required string ConnectionString { get; set; }

        public bool UseLazyLoading { get; set; } = true;
    }
}
