using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Shared.Enums;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public sealed class ClientConnectionMonitor : MonoBehaviour
    {
        private NetworkManager _network;
        private ulong _clientId;
        private bool _monitoring;
        private bool _disconnectPending;

        public void Initialize(NetworkManager network)
        {
            StopMonitoring();

            _network = network;
            _clientId = network.LocalClientId;
            _monitoring = true;
            _disconnectPending = false;
            network.OnClientDisconnectCallback += HandleDisconnected;
            network.OnTransportFailure += HandleTransportFailure;
            UserManager.Instance.SessionInvalidated += HandleSessionInvalidated;

            // Startup owns rollback until BootstrapScene has unloaded. Catch a loss
            // during that final scene transition as well as future disconnects.
            if (!network.IsConnectedClient)
            {
                HandleDisconnected(_clientId);
            }
        }

        private void HandleDisconnected(ulong clientId)
        {
            if (clientId == _clientId)
            {
                RequestReturnToLogin("Disconnected");
            }
        }

        private void HandleTransportFailure()
        {
            RequestReturnToLogin("TransportFailure");
        }

        private void RequestReturnToLogin(string reason)
        {
            if (!_monitoring || _disconnectPending)
            {
                return;
            }

            _disconnectPending = true;
            Debug.LogWarning($"Client connection lost. ClientId: {_clientId}, CharacterId: {UserManager.Instance.SelectedCharacterId}, Reason: {reason}. Returning to login.");

            ReturnToLoginAsync().Forget();
        }

        private async UniTask ReturnToLoginAsync()
        {
            // Leave the NGO callback before initiating shutdown/scene teardown.
            var canceled = await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy())
                .SuppressCancellationThrow();

            if (canceled || !_monitoring)
            {
                return;
            }

            StopMonitoring();

            var message = TranslateManager.Instance.GetByKey(TranslateKeyEnum.ConnectionLost);

            UserManager.Instance.InvalidateSession(message);
        }

        private void HandleSessionInvalidated(string message)
        {
            StopMonitoring();
        }

        private void StopMonitoring()
        {
            _monitoring = false;

            if (_network != null)
            {
                _network.OnClientDisconnectCallback -= HandleDisconnected;
                _network.OnTransportFailure -= HandleTransportFailure;
            }

            UserManager.Instance.SessionInvalidated -= HandleSessionInvalidated;
        }

        private void OnApplicationQuit()
        {
            StopMonitoring();
        }

        private void OnDestroy()
        {
            StopMonitoring();
        }
    }
}
