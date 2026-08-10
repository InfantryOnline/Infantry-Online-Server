using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseServerNATS.Handlers.ZoneState
{
    /// <summary>
    /// Handles the initial zone connection.
    /// </summary>
    /// <remarks>
    /// We issue a session token that the zone server must use to communicate from here on out.
    /// If this zone token is not present, we send back a response saying the zone has to
    /// authorize first.
    /// 
    /// If the zone server is already connected, we drop its instance and start fresh.
    /// </remarks>
    [NatsSubject("database.zone.connect")]
    public class ZoneConnectHandler : INatsRpcHandler<string, string>
    {
        public Task<string> HandleAsync(NatsRequestContext<string> ctx, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
