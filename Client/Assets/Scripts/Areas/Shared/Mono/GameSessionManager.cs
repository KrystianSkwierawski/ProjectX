using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Shared.Models;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public static class GameSessionManager
    {
        private const string _dtlsConnectionType = "dtls";
        private const int _connectionApprovalTimeoutSeconds = 20;
        private const int _maxConnections = 100;
        private const int _maxTicketPayloadBytes = 256;
        private const int _minimumTicketValiditySeconds = 20;
        private const int _revokeRetryCount = 3;
        private const int _serverHeartbeatMaximumIntervalSeconds = 30;
        private const int _serverHeartbeatRetrySeconds = 10;
        private const int _serverLeaseSafetyMarginSeconds = 5;
        private const int _serverHeartbeatSchedulingCushionSeconds = 1;

        private static readonly HashSet<ulong> _disconnectedDuringApproval = new HashSet<ulong>();
        private static readonly HashSet<ulong> _pendingApprovals = new HashSet<ulong>();
        private static Guid _serverGameSessionId;

        public static async UniTask StartServerAsync()
        {
            var networkManager = GetNetworkManager();

            _serverGameSessionId = Guid.Empty;
            _disconnectedDuringApproval.Clear();
            _pendingApprovals.Clear();
            ConfigureConnectionApproval(networkManager);

            var usesRelay = ShouldUseRelay();
            var relayJoinCode = usesRelay ? await ConfigureRelayServerAsync(networkManager) : null;

            networkManager.OnClientDisconnectCallback -= HandleServerClientDisconnected;
            networkManager.OnClientDisconnectCallback += HandleServerClientDisconnected;
            networkManager.OnTransportFailure -= HandleServerTransportFailure;
            networkManager.OnTransportFailure += HandleServerTransportFailure;

            if (!networkManager.StartServer())
            {
                networkManager.OnClientDisconnectCallback -= HandleServerClientDisconnected;
                networkManager.OnTransportFailure -= HandleServerTransportFailure;
                throw new InvalidOperationException("The network server could not be started.");
            }

            try
            {
                var command = new RegisterGameSessionCommand { UsesRelay = usesRelay, RelayJoinCode = relayJoinCode };

                var registration = await UnityWebRequestHelper.ExecutePostAsync<RegisterGameSessionDto>("GameSessions/Register", command, log: false);

                if (registration == null || registration.GameSessionId == Guid.Empty
                    || registration.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddSeconds(_serverLeaseSafetyMarginSeconds))
                {
                    throw new InvalidOperationException("The API did not register the game session.");
                }

                _serverGameSessionId = registration.GameSessionId;

                MaintainServerLeaseAsync(networkManager, registration.ExpiresAtUtc).Forget();

                Debug.Log(usesRelay ? "Server started through Unity Relay with DTLS." : "Server started with the explicit local direct transport.");
            }
            catch
            {
                networkManager.OnClientDisconnectCallback -= HandleServerClientDisconnected;
                networkManager.OnTransportFailure -= HandleServerTransportFailure;
                networkManager.Shutdown();
                throw;
            }
        }

        public static async UniTask StartClientAsync()
        {
            var networkManager = GetNetworkManager();

            // Initialize UGS before issuing the short-lived ticket so first-run latency stays outside its validity window.
            if (ShouldUseRelay())
            {
                await InitializeUnityServicesAsync();
            }

            var ticket = await RequestTicketAsync();

            await ConfigureClientTransportAsync(networkManager, ticket);

            if (TicketExpiresSoon(ticket))
            {
                var freshTicket = await RequestTicketAsync();

                if (!UsesSameRoute(ticket, freshTicket))
                {
                    await ConfigureClientTransportAsync(networkManager, freshTicket);
                }

                ticket = freshTicket;
            }

            if (TicketExpiresSoon(ticket))
            {
                throw new InvalidOperationException("The API returned a connection ticket without enough remaining validity.");
            }

            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.NetworkConfig.ClientConnectionBufferTimeout = _connectionApprovalTimeoutSeconds;
            networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(ticket.Ticket);

            var disconnected = false;

            void HandleClientDisconnected(ulong _) => disconnected = true;

            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;

            try
            {
                if (!networkManager.StartClient())
                {
                    throw new InvalidOperationException("The network client could not be started.");
                }

                var deadline = Time.realtimeSinceStartupAsDouble + _connectionApprovalTimeoutSeconds;

                while (!networkManager.IsConnectedClient && !disconnected)
                {
                    if (Time.realtimeSinceStartupAsDouble >= deadline)
                    {
                        throw new TimeoutException("The game server did not approve the connection in time.");
                    }

                    await UniTask.Yield();
                }

                if (!networkManager.IsConnectedClient)
                {
                    throw new InvalidOperationException("The game server rejected the connection ticket.");
                }
            }
            catch
            {
                if (networkManager.IsListening)
                {
                    networkManager.Shutdown();
                }

                throw;
            }
            finally
            {
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                networkManager.NetworkConfig.ConnectionData = Array.Empty<byte>();
            }

            Debug.Log(ticket.UsesRelay ? "Client started through Unity Relay with DTLS." : "Client started with the explicit local direct transport.");
        }

        private static void ConfigureConnectionApproval(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.NetworkConfig.ClientConnectionBufferTimeout = _connectionApprovalTimeoutSeconds;
            networkManager.ConnectionApprovalCallback = HandleConnectionApproval;
        }

        private static void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            _disconnectedDuringApproval.Remove(request.ClientNetworkId);
            _pendingApprovals.Add(request.ClientNetworkId);

            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Pending = true;

            ApproveConnectionAsync(request, response).Forget();
        }

        private static async UniTaskVoid ApproveConnectionAsync(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            try
            {
                if (request.Payload == null || request.Payload.Length == 0 || request.Payload.Length > _maxTicketPayloadBytes)
                {
                    throw new InvalidOperationException("The connection ticket payload is invalid.");
                }

                var ticket = Encoding.UTF8.GetString(request.Payload);

                if (_serverGameSessionId == Guid.Empty || string.IsNullOrWhiteSpace(ticket))
                {
                    throw new InvalidOperationException("The connection ticket is missing.");
                }

                var command = new RedeemGameSessionTicketCommand { GameSessionId = _serverGameSessionId, Ticket = ticket };

                var redeemed = await UnityWebRequestHelper.ExecutePostAsync<RedeemGameSessionTicketDto>("GameSessions/Redeem", command, log: false);

                if (redeemed == null || string.IsNullOrWhiteSpace(redeemed.PlayerSessionId))
                {
                    throw new InvalidOperationException("The API did not create a player session.");
                }

                if (_disconnectedDuringApproval.Remove(request.ClientNetworkId) || NetworkManager.Singleton == null
                    || !NetworkManager.Singleton.IsListening)
                {
                    await RevokePlayerSessionAsync(redeemed.PlayerSessionId);
                    throw new InvalidOperationException("The client disconnected before connection approval completed.");
                }

                UserManager.Instance.SetPlayerSessionId(request.ClientNetworkId, redeemed.PlayerSessionId);

                response.Approved = true;
                response.CreatePlayerObject = true;
                response.Reason = string.Empty;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Connection approval rejected for client {request.ClientNetworkId}: {exception.Message}");

                response.Approved = false;
                response.CreatePlayerObject = false;
                response.Reason = "Game session authentication failed.";
            }
            finally
            {
                _disconnectedDuringApproval.Remove(request.ClientNetworkId);
                _pendingApprovals.Remove(request.ClientNetworkId);

                response.Pending = false;
            }
        }

        private static async UniTask<GameSessionTicketDto> RequestTicketAsync()
        {
            var ticket = await UnityWebRequestHelper.ExecutePostAsync<GameSessionTicketDto>("GameSessions/Ticket", log: false);

            if (ticket == null || ticket.GameSessionId == Guid.Empty || string.IsNullOrWhiteSpace(ticket.Ticket)
                || ticket.ExpiresAtUtc == default)
            {
                throw new InvalidOperationException("The API did not return a valid game connection ticket.");
            }

            return ticket;
        }

        private static async UniTask ConfigureClientTransportAsync(NetworkManager networkManager, GameSessionTicketDto ticket)
        {
            if (ticket.UsesRelay)
            {
                if (string.IsNullOrWhiteSpace(ticket.RelayJoinCode))
                {
                    throw new InvalidOperationException("The Relay game session has no join code.");
                }

                await ConfigureRelayClientAsync(networkManager, ticket.RelayJoinCode);

                return;
            }

            if (!DirectTransportIsAllowed())
            {
                throw new InvalidOperationException("The server advertised an insecure direct session, but this client requires Relay with DTLS.");
            }

            var transport = GetUnityTransport(networkManager);
            var directConnection = transport.ConnectionData;
            transport.SetConnectionData(directConnection.Address, directConnection.Port, directConnection.ServerListenAddress);
        }

        private static bool TicketExpiresSoon(GameSessionTicketDto ticket)
        {
            return ticket.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddSeconds(_minimumTicketValiditySeconds);
        }

        private static bool UsesSameRoute(GameSessionTicketDto first, GameSessionTicketDto second)
        {
            return first.GameSessionId == second.GameSessionId
                && first.UsesRelay == second.UsesRelay
                && string.Equals(first.RelayJoinCode, second.RelayJoinCode, StringComparison.Ordinal);
        }

        private static async UniTask<string> ConfigureRelayServerAsync(NetworkManager networkManager)
        {
            await InitializeUnityServicesAsync();

            var allocation = await RelayService.Instance.CreateAllocationAsync(_maxConnections);

            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = GetUnityTransport(networkManager);

            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, _dtlsConnectionType));

            return joinCode;
        }

        private static async UniTask ConfigureRelayClientAsync(NetworkManager networkManager, string relayJoinCode)
        {
            await InitializeUnityServicesAsync();

            var allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            var transport = GetUnityTransport(networkManager);

            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, _dtlsConnectionType));
        }

        private static async UniTask InitializeUnityServicesAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Initializing)
            {
                await UniTask.WaitUntil(() => UnityServices.State != ServicesInitializationState.Initializing);
            }

            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions()
                    .SetEnvironmentName(GetCommandLineValue("-projectx-ugs-environment", "production"))
                    .SetProfile(GetUgsProfile());

                await UnityServices.InitializeAsync(options);
            }

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                throw new InvalidOperationException("Unity Gaming Services could not be initialized.");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        private static void HandleServerClientDisconnected(ulong clientId)
        {
            if (_pendingApprovals.Contains(clientId))
            {
                _disconnectedDuringApproval.Add(clientId);
            }

            if (!UserManager.Instance.TryTakePlayerSessionId(clientId, out var playerSessionId))
            {
                return;
            }

            RevokePlayerSessionAsync(playerSessionId).Forget();
        }

        private static void HandleServerTransportFailure()
        {
            Debug.LogError("The dedicated server transport failed; terminating for supervised restart.");

            Application.Quit(1);
        }

        private static async UniTask RevokePlayerSessionAsync(string playerSessionId)
        {
            for (var attempt = 1; attempt <= _revokeRetryCount; attempt++)
            {
                try
                {
                    var command = new RevokePlayerSessionCommand { PlayerSessionId = playerSessionId };

                    await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("GameSessions/RevokePlayer", command, log: false);

                    return;
                }
                catch (Exception exception)
                {
                    if (attempt == _revokeRetryCount)
                    {
                        Debug.LogWarning($"Player-session revocation failed after {attempt} attempts: {exception.Message}");
                        return;
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(attempt), ignoreTimeScale: true);
                }
            }
        }

        private static async UniTaskVoid MaintainServerLeaseAsync(NetworkManager networkManager, DateTimeOffset expiresAtUtc)
        {
            var retrying = false;
            var safetyMargin = TimeSpan.FromSeconds(_serverLeaseSafetyMarginSeconds);
            var requestReserve = TimeSpan.FromSeconds(UnityWebRequestHelper.RequestTimeoutSeconds + _serverLeaseSafetyMarginSeconds);
            var schedulingCushion = TimeSpan.FromSeconds(_serverHeartbeatSchedulingCushionSeconds);

            while (networkManager != null && networkManager.IsListening)
            {
                var remaining = expiresAtUtc - DateTimeOffset.UtcNow;

                if (remaining <= requestReserve + schedulingCushion)
                {
                    FailServerLease(networkManager, new TimeoutException("The game-session lease could not be renewed before expiry."));
                    return;
                }

                var availableDelay = remaining - requestReserve - schedulingCushion;
                var preferredDelay = GetPreferredHeartbeatDelay(remaining, retrying);
                var delay = preferredDelay <= availableDelay ? preferredDelay : availableDelay;

                await UniTask.Delay(delay, ignoreTimeScale: true);

                if (networkManager == null || !networkManager.IsListening)
                {
                    return;
                }

                remaining = expiresAtUtc - DateTimeOffset.UtcNow;

                if (remaining <= requestReserve)
                {
                    FailServerLease(networkManager, new TimeoutException("The game-session lease could not be renewed before expiry."));
                    return;
                }

                try
                {
                    using var requestCancellation = new System.Threading.CancellationTokenSource(remaining - safetyMargin);
                    var command = new HeartbeatGameSessionCommand { GameSessionId = _serverGameSessionId };

                    var heartbeat = await UnityWebRequestHelper.ExecutePostAsync<HeartbeatGameSessionDto>("GameSessions/Heartbeat", command, log: false, cancellationToken: requestCancellation.Token);

                    if (heartbeat == null || heartbeat.GameSessionId != _serverGameSessionId
                        || heartbeat.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddSeconds(_serverLeaseSafetyMarginSeconds))
                    {
                        throw new InvalidOperationException("The API returned an invalid game-session heartbeat response.");
                    }

                    expiresAtUtc = heartbeat.ExpiresAtUtc;
                    retrying = false;
                }
                catch (Exception exception)
                {
                    if (exception is ApiRequestException apiException && apiException.ResponseCode >= 400 && apiException.ResponseCode < 500)
                    {
                        FailServerLease(networkManager, exception);
                        return;
                    }

                    if (DateTimeOffset.UtcNow >= expiresAtUtc - safetyMargin)
                    {
                        FailServerLease(networkManager, exception);
                        return;
                    }

                    retrying = true;
                    Debug.LogWarning($"Game-session heartbeat failed; retrying before the lease expires: {exception.Message}");
                }
            }
        }

        private static TimeSpan GetPreferredHeartbeatDelay(TimeSpan remaining, bool retrying)
        {
            if (retrying)
            {
                return TimeSpan.FromSeconds(_serverHeartbeatRetrySeconds);
            }

            var maximumInterval = TimeSpan.FromSeconds(_serverHeartbeatMaximumIntervalSeconds);
            var halfRemainingLease = TimeSpan.FromTicks(remaining.Ticks / 2);

            return maximumInterval <= halfRemainingLease ? maximumInterval : halfRemainingLease;
        }

        private static void FailServerLease(NetworkManager networkManager, Exception exception)
        {
            Debug.LogError($"Game-session lease was lost: {exception.Message}");

            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            Application.Quit(1);
        }

        private static NetworkManager GetNetworkManager()
        {
            return NetworkManager.Singleton != null ? NetworkManager.Singleton : throw new InvalidOperationException("NetworkManager is not available.");
        }

        private static UnityTransport GetUnityTransport(NetworkManager networkManager)
        {
            var transport = networkManager.GetComponent<UnityTransport>();

            return transport != null ? transport : throw new InvalidOperationException("UnityTransport is not available.");
        }

        private static bool ShouldUseRelay()
        {
            if (HasCommandLineSwitch("-projectx-relay"))
            {
                return true;
            }

            return !DirectTransportIsAllowed();
        }

        private static bool DirectTransportIsAllowed()
        {
            if (HasCommandLineSwitch("-projectx-relay"))
            {
                return false;
            }

            return Application.isEditor || HasCommandLineSwitch("-projectx-direct");
        }

        private static string GetUgsProfile()
        {
            var configuredProfile = GetCommandLineValue("-projectx-ugs-profile", null);

            if (!string.IsNullOrWhiteSpace(configuredProfile))
            {
                return configuredProfile;
            }

#if UNITY_EDITOR
            return Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor
                ? "projectx-client-main"
                : "projectx-client-secondary";
#elif UNITY_SERVER
            return "projectx-server";
#else
            return "projectx-client";
#endif
        }

        private static bool HasCommandLineSwitch(string value)
        {
            return Environment.GetCommandLineArgs().Any(argument => string.Equals(argument, value, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetCommandLineValue(string name, string fallback)
        {
            var arguments = Environment.GetCommandLineArgs();

            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return fallback;
        }
    }
}
