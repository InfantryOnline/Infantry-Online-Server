using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaemonConsole
{
    public class BaseConfiguration
    {
        public string DaemonPipeName { get; set; }

        public Repository Repository { get; set; } = new Repository();
    }

    public class Repository
    {
        public string GitHubOwnerName { get; set; }

        public string GitHubRepositoryName { get; set; }

        public string ZoneServerLinuxPackageName { get; set; }

        public string ZoneServerWindowsPackageName { get; set; }

        public string InfrastructureWindowsPackageName { get; set; }

        public string InfrastructureLinuxPackageName { get; set; }
    }
}
