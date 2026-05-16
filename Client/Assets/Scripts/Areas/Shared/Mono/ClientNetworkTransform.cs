using Unity.Netcode.Components;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}