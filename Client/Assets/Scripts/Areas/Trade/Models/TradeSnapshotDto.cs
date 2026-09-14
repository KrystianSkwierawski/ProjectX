using System;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Trade.Models
{
    public class TradeSnapshotDto
    {
        public TradeInvitationDto Invitation { get; set; }

        public TradeInvitationDto OutgoingInvitation { get; set; }

        public bool HasSession { get; set; }

        public string PartnerName { get; set; }

        public InventoryItemDto[] MyOffer { get; set; } = Array.Empty<InventoryItemDto>();

        public InventoryItemDto[] PartnerOffer { get; set; } = Array.Empty<InventoryItemDto>();

        public bool MyLocked { get; set; }

        public bool PartnerLocked { get; set; }

        public bool MyConfirmed { get; set; }

        public bool PartnerConfirmed { get; set; }

        public bool IsCommitting { get; set; }
    }
}
