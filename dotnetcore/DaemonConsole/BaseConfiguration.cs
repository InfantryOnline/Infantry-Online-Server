using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaemonConsole
{
    public class BaseConfiguration
    {
        public SystemPathsConfiguration SystemPaths { get; set; } = new SystemPathsConfiguration();

        public RepositoryConfiguration Repository { get; set; } = new RepositoryConfiguration();

        public ZoneKitConfiguration ZoneKit { get; set; } = new ZoneKitConfiguration();
    }

    public class ZoneKitConfiguration
    {
        public string BaseUrl { get; set; }
    }

    public class SystemPathsConfiguration
    {
        public string DaemonPipeName { get; set; }

        public string DaemonProcessPath { get; set; }

        public string ZonesFolderPath { get; set; }
    }

    public class RepositoryConfiguration
    {
        public string GitHubOwnerName { get; set; }

        public string GitHubRepositoryName { get; set; }

        public string ZoneServerLinuxPackageName { get; set; }

        public string ZoneServerWindowsPackageName { get; set; }

        public string InfrastructureWindowsPackageName { get; set; }

        public string InfrastructureLinuxPackageName { get; set; }
    }
}
