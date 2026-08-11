using System;

namespace Assets.Scripts.Areas.Shared.Models
{
    public sealed class RedeemGameSessionTicketCommand
    {
        public Guid GameSessionId { get; set; }

        public string Ticket { get; set; }
    }
}
