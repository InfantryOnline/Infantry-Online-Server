using Castle.Core.Logging;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DatabaseServerNATS.Handlers.Database
{
    /// <summary>
    /// Simple health check over NATS.
    /// </summary>
    [NatsSubject("database.health")]
    public class HealthCheckHandler(ILogger<HealthCheckHandler> logger, InfantryDbContext db) : INatsRpcHandler<string, string>
    {
        public async Task<string> HandleAsync(NatsRequestContext<string> ctx, CancellationToken ct)
        {
            try
            {
                var result = await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
                return "Not Healthy.";
            }

            return "Healthy.";
        }
    }
}
