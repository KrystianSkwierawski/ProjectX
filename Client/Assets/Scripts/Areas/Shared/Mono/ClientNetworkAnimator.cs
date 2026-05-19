using Unity.Netcode.Components;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public class ClientNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}