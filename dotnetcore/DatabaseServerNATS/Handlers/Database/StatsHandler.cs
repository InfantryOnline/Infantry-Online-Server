using Castle.Core.Logging;
using Database;
using DatabaseServerNATS.Application;
using DatabaseServerNATS.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseServerNATS.Handlers.Database
{
    /// <summary>
    /// Prints out a bit of useful stats.
    /// </summary>
    [NatsSubject("database.stats")]
    public class StatsHandler(ILogger<StatsHandler> logger, ServerStore store, InfantryDbContext db) : INatsRpcHandler<string, string>
    {
        public async Task<string> HandleAsync(NatsRequestContext<string> ctx, CancellationToken ct)
        {
            var zones = await db.Zones.CountAsync(ct);
            var accounts = await db.Accounts.CountAsync(ct);

            return $"Uptime: {store.TimeStarted.ToRoundedString()}, Accounts: {accounts}, Zones: {zones}";
        }
    }
}
