using NATS.Client.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseServerNATS.Handlers
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class NatsSubjectAttribute : Attribute
    {
        public string Subject { get; }
        public NatsSubjectAttribute(string subject) => Subject = subject;
    }

    /// <summary>
    /// Context around a particular event, used by handlers.
    /// </summary>
    public record NatsRequestContext<TReq>(
        TReq Payload,
        string Subject,
        string? ReplyTo,
        INatsHeaders? Headers
    );

    /// <summary>
    /// Main interface for all NATS-based Request-Response events.
    /// </summary>
    public interface INatsRpcHandler<TReq, TResp>
    {
        Task<TResp> HandleAsync(NatsRequestContext<TReq> ctx, CancellationToken ct);
    }
}
