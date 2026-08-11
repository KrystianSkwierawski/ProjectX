namespace Assets.Scripts.Areas.Shared.Models
{
    public sealed class RegisterGameSessionCommand
    {
        public bool UsesRelay { get; set; }

        public string RelayJoinCode { get; set; }
    }
}
