using DatabaseServerNATS.Application;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseServerNATS.Handlers.ZoneState
{
    /// <summary>
    /// Zone lets the database server know it is still alive.
    /// </summary>
    [NatsSubject("database.zone.heartbeat")]
    public class ZoneHeartbeatHandler(ILogger<ZoneHeartbeatHandler> logger, ServerStore store) : INatsRpcHandler<string, string> 
    {
        public Task<string> HandleAsync(NatsRequestContext<string> ctx, CancellationToken ct)
        {
            try
            {
                var sessionId = ctx.Headers["Zone-Session-Id"];

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    // Zone tried to send a heartbeat after timing out, but without re-authorizing. Erronous state.

                    return Task.FromResult(string.Empty);
                }

                var zone = store.ZoneServers[sessionId];

                zone.LastHeartbeat.Restart();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }

            return Task.FromResult(string.Empty);
        }
    }
}
