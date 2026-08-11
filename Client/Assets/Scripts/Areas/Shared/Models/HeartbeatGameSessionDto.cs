using System;

namespace Assets.Scripts.Areas.Shared.Models
{
    public sealed class HeartbeatGameSessionDto
    {
        public Guid GameSessionId { get; set; }

        public DateTimeOffset ExpiresAtUtc { get; set; }
    }
}
