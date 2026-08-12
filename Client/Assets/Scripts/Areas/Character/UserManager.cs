using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Professions.Enums;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Areas.Character
{
    public class UserManager : Singleton<UserManager>
    {
        private static readonly TimeSpan _sessionRefreshInterval = TimeSpan.FromMinutes(56);
        private static readonly TimeSpan _sessionRefreshGracePeriod = TimeSpan.FromMinutes(4);
        private static readonly TimeSpan _sessionRefreshRetryInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan _sessionMaximumLifetime = TimeSpan.FromHours(24);

        private readonly IDictionary<ulong, string> _playerSessionIds = new Dictionary<ulong, string>();
        private CancellationTokenSource _sessionRefreshCancellation;
        private double _sessionExpiresAtRealtime;

        public IDictionary<ulong, CharacterDto> Characters { get; } = new Dictionary<ulong, CharacterDto>();

        public string Token { get; private set; }

        public LanguageEnum Language { get; private set; }

        public ulong OwnerClientId { get; set; } // TODO: replace all references

        public event Action<string> SessionInvalidated;

        public async UniTask LoginAsync(string userName, string password, CancellationToken cancellationToken = default)
        {
            var result = await UnityWebRequestHelper.ExecutePostAsync<LoginApplicationUserDto>("ApplicationUsers", new LoginApplicationUserCommand
            {
                UserName = userName,
                Password = password
            }, log: false, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(result?.Token))
            {
                throw new InvalidOperationException("The login response did not contain an access token.");
            }

            Token = result.Token;
            Language = result.Language;
            _sessionExpiresAtRealtime = Time.realtimeSinceStartupAsDouble + _sessionMaximumLifetime.TotalSeconds;

            StartSessionRefresh();

            Debug.Log($"Login -> UserName: {userName}, Language: {Language}");
        }

        private void StartSessionRefresh()
        {
            _sessionRefreshCancellation?.Cancel();
            _sessionRefreshCancellation?.Dispose();
            _sessionRefreshCancellation = new CancellationTokenSource();

            RefreshSessionPeriodicallyAsync(_sessionRefreshCancellation.Token).Forget();
        }

        private async UniTask RefreshSessionPeriodicallyAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    var sessionRemainingSeconds = _sessionExpiresAtRealtime - Time.realtimeSinceStartupAsDouble;

                    if (sessionRemainingSeconds <= 0)
                    {
                        InvalidateSession();
                        return;
                    }

                    var refreshDelay = TimeSpan.FromSeconds(Math.Min(_sessionRefreshInterval.TotalSeconds, sessionRemainingSeconds));

                    await UniTask.Delay(refreshDelay, ignoreTimeScale: true, cancellationToken: cancellationToken);

                    if (Time.realtimeSinceStartupAsDouble >= _sessionExpiresAtRealtime)
                    {
                        InvalidateSession();
                        return;
                    }

                    if (!await RefreshSessionWithRetryAsync(cancellationToken))
                    {
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        private async UniTask<bool> RefreshSessionWithRetryAsync(CancellationToken cancellationToken)
        {
            var refreshDeadline = Time.realtimeSinceStartupAsDouble + _sessionRefreshGracePeriod.TotalSeconds;

            while (true)
            {
                if (Time.realtimeSinceStartupAsDouble >= refreshDeadline)
                {
                    InvalidateSession();

                    return false;
                }

                try
                {
                    var result = await UnityWebRequestHelper.ExecutePostAsync<LoginApplicationUserDto>("ApplicationUsers/RefreshSession", log: false,
                        cancellationToken: cancellationToken);

                    if (string.IsNullOrWhiteSpace(result?.Token))
                    {
                        throw new InvalidOperationException("The session refresh response did not contain an access token.");
                    }

                    Token = result.Token;
                    Language = result.Language;

                    Debug.Log("Session refreshed.");

                    return true;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (ApiRequestException exception)
                {
                    if (exception.ResponseCode >= 400 && exception.ResponseCode < 500)
                    {
                        Debug.LogWarning($"Session refresh was rejected. HTTP: {exception.ResponseCode}, Error: {exception.Message}.");

                        InvalidateSession();

                        return false;
                    }

                    Debug.LogWarning($"Session refresh failed. HTTP: {exception.ResponseCode}, Error: {exception.Message}. Retrying shortly.");
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Session refresh failed: {exception.Message}. Retrying shortly.");
                }

                var remainingSeconds = refreshDeadline - Time.realtimeSinceStartupAsDouble;

                if (remainingSeconds <= 0)
                {
                    InvalidateSession();

                    return false;
                }

                var retryDelay = TimeSpan.FromSeconds(Math.Min(_sessionRefreshRetryInterval.TotalSeconds, remainingSeconds));

                await UniTask.Delay(retryDelay, ignoreTimeScale: true, cancellationToken: cancellationToken);
            }
        }

        private void InvalidateSession()
        {
            _sessionRefreshCancellation?.Cancel();

            Token = null;
            Language = default;
            OwnerClientId = default;
            _sessionExpiresAtRealtime = default;
            Characters.Clear();
            _playerSessionIds.Clear();

            const string message = "Your session could not be refreshed. Please sign in again.";

#if UNITY_SERVER && !UNITY_EDITOR
            Debug.LogError(message);

            Application.Quit(1);
#else
            SessionInvalidated?.Invoke(message);
#endif
        }

        public void SetPlayerSessionId(ulong clientId, string playerSessionId)
        {
            if (string.IsNullOrWhiteSpace(playerSessionId))
            {
                throw new ArgumentException("A player session ID is required.", nameof(playerSessionId));
            }

            _playerSessionIds[clientId] = playerSessionId;
        }

        public string GetPlayerSessionId(ulong clientId)
        {
            return _playerSessionIds.TryGetValue(clientId, out var playerSessionId)
                ? playerSessionId
                : throw new InvalidOperationException($"No authenticated player session exists for network client {clientId}.");
        }

        public bool TryTakePlayerSessionId(ulong clientId, out string playerSessionId)
        {
            if (!_playerSessionIds.TryGetValue(clientId, out playerSessionId))
            {
                return false;
            }

            _playerSessionIds.Remove(clientId);

            return true;
        }

        public byte GetLevelByRecipeType(CraftingRecipeTypeEnum craftingRecipeType)
        {
            return GetLevelByRecipeType(craftingRecipeType, NetworkManager.Singleton.LocalClientId);
        }

        public byte GetLevelByRecipeType(CraftingRecipeTypeEnum craftingRecipeType, ulong clientId)
        {
            var type = craftingRecipeType switch
            {
                CraftingRecipeTypeEnum.Cooking => ExperienceTypeEnum.Cooking,
                CraftingRecipeTypeEnum.Blacksmithing => ExperienceTypeEnum.Blacksmithing,
                CraftingRecipeTypeEnum.Alchemy => ExperienceTypeEnum.Alchemy,
                _ => ExperienceTypeEnum.None,
            };

            if (type == ExperienceTypeEnum.None)
            {
                return 0;
            }

            return Characters[clientId].Levels[type];
        }
    }
}
