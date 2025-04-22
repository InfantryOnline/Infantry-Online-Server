using Octokit;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaemonConsole
{
    /// <summary>
    /// Allows for downloading of releases for zones and infrastructure files.
    /// </summary>
    /// <param name="configuration"></param>
    internal class ZoneKitDownloader(BaseConfiguration configuration)
    {
        public async Task<ZipArchive> FetchZoneKitByName(string kitName)
        {
            using var http = new HttpClient();

            Console.WriteLine($"Downloading latest zone kit {kitName}.zip...");

            var remoteStream = await http.GetStreamAsync($"{configuration.ZoneKit.BaseUrl}/{kitName}.zip");
            var outputStream = new MemoryStream();

            await remoteStream.CopyToAsync(outputStream);
            outputStream.Position = 0;

            Console.WriteLine($"Zone Kit downloaded ({outputStream.Length} bytes)");

            var archive = new ZipArchive(outputStream);

            return archive;
        }
    }
}
