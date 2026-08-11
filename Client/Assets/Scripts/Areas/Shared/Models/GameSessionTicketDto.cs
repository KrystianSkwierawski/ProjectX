using System;

namespace Assets.Scripts.Areas.Shared.Models
{
    public sealed class GameSessionTicketDto
    {
        public Guid GameSessionId { get; set; }

        public bool UsesRelay { get; set; }

        public string RelayJoinCode { get; set; }

        public string Ticket { get; set; }

        public DateTimeOffset ExpiresAtUtc { get; set; }
    }
}
