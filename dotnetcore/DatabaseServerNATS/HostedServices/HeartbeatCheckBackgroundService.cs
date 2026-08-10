using DatabaseServerNATS.Application;
using DatabaseServerNATS.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseServerNATS.HostedServices
{
    /// <summary>
    /// Checks the heartbeat of zone servers, evicting them if they have not sent a heartbeat in a suitable amount of time.
    /// </summary>
    public class HeartbeatCheckBackgroundService (ILogger<HeartbeatCheckBackgroundService> logger, IOptions<ZoneServerOptions> zoneOpts, ServerStore store) : BackgroundService
    {
        private TimeSpan period => TimeSpan.FromSeconds(1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(period);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    foreach(var (id, zone) in store.ZoneServers)
                    {
                        if (zone.LastHeartbeat.ElapsedMilliseconds > zoneOpts.Value.HeartbeatIntervalMs)
                        {
                            // Zone has not responded in a suitable time, evict it and clean up any residual state from it.

                            ZoneServer outz;
                            store.ZoneServers.Remove(id, out outz);

                            // TODO: Clear out, clean up the empty chat channels.
                        }
                    }
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
            }
        }
    }
}
