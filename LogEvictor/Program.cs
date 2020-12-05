using System;
using System.IO;
using System.Linq;

namespace LogEvictor
{
    /// <summary>
    /// Deletes logs that are more than 5 days old.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: Move this to some place, eventually.
            Directory.GetDirectories("C:/Infantry/Zones").ToList().ForEach(EvictZoneLogs);
        }
        
        static void EvictZoneLogs(string zoneDir)
        {
            var path = Path.Combine(zoneDir, "oldlogs");

            if (Directory.Exists(path))
            {
                Directory.GetDirectories(path).ToList().ForEach(MaybeDelete);
            }
        }

        static void MaybeDelete(string logDir)
        {
            if (Directory.GetCreationTime(logDir) < DateTime.Today.AddDays(-5))
            {
                Directory.Delete(logDir, true);
            }
        }
    }
}
