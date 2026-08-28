using Unity.Netcode;

namespace Assets.Scripts.Areas.Shared.Extensions
{
    public static class ClientRpcParamsExtensions
    {
        public static ClientRpcParams ToClientRpcParams(this ulong clientId)
        {
            return new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };
        }
    }
}
