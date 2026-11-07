using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.Mono;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Combat;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectX.Editor
{
    [InitializeOnLoad]
    public static class DamageMeterGameplayValidation
    {
        private const string PendingKey = "ProjectX.DamageMeterGameplayValidation";
        private const string PlayerPrefabPath = "Assets/Prefabs/Character/NestedParentArmature_Unpack Variant.prefab";

        static DamageMeterGameplayValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
                {
                    SessionState.SetBool(PendingKey, false);
                    ValidateSoloDeliveryAsync().Forget();
                }
            };
        }

        [MenuItem("ProjectX/Combat/Validate Damage Meter Gameplay")]
        public static void Run()
        {
            Require(!Application.isPlaying, "Run damage meter validation outside Play Mode.");

            ValidateAccumulator();
            ValidateEffectiveDamage();
            ValidatePartyRouting();
            ValidatePlayerHierarchy();

            Debug.Log("DAMAGE_METER_GAMEPLAY_VALIDATED: accumulation, player/weapon grouping, stable sorting, invalid damage, local reset, overkill calculation, current party routing and actual player prefab hierarchy.");
        }

        [MenuItem("ProjectX/Combat/Validate Damage Meter Solo Delivery (Play Mode)")]
        public static void RunPlayMode()
        {
            Run();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void ValidatePlayerHierarchy()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            Require(prefab != null && prefab.GetComponent<NetworkObject>() != null,
                "The configured gameplay player prefab must have its NGO root.");
            Require(prefab.GetComponent<Player>() == null && prefab.GetComponentInChildren<Player>() != null,
                "Regression fixture must retain the real player hierarchy: NGO root with a moving Player child.");
        }

        private static async UniTask ValidateSoloDeliveryAsync()
        {
            NetworkManager network = null;

            try
            {
                var networkObject = new GameObject("DamageMeterProbeNetwork");
                var transport = networkObject.AddComponent<UnityTransport>();
                transport.SetConnectionData("127.0.0.1", 0);

                network = networkObject.AddComponent<NetworkManager>();
                network.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = transport,
                    EnableSceneManagement = false
                };

                Require(network.StartHost(), "Solo delivery probe must start a real NGO host.");

                await UniTask.WaitUntil(() => network.IsConnectedClient).Timeout(TimeSpan.FromSeconds(10));

                // Match the checked-in prefab's root/child relationship, without starting
                // unrelated authenticated character loading, UI or persistence systems.
                var root = new GameObject("DamageMeterProbePlayerRoot");
                var playerNetwork = root.AddComponent<NetworkObject>();
                var child = new GameObject("Player");
                child.transform.SetParent(root.transform);
                var player = child.AddComponent<Player>();
                player.enabled = false;

                var travel = child.AddComponent<DungeonTravel>();
                travel.enabled = false;
                playerNetwork.SpawnAsPlayerObject(network.LocalClientId);

                Require(player.IsSpawned && player.IsOwner && DungeonTravel.CanInteract(network.LocalClientId),
                    "The actual Player and dungeon lifetime must be spawned for the local owner.");
                Require(network.ConnectedClients[network.LocalClientId].PlayerObject == playerNetwork
                    && playerNetwork.GetComponent<Player>() == null,
                    "NGO must resolve the root, not the moving Player child, as its PlayerObject.");

                UserManager.Instance.Characters[network.LocalClientId] = new CharacterDto
                {
                    Id = 21,
                    Name = "Solo probe"
                };

                var target = new GameObject("DamageMeterProbeTarget");
                var targetNetwork = target.AddComponent<NetworkObject>();
                var health = target.AddComponent<Health>();
                health.enabled = false;
                targetNetwork.Spawn();
                typeof(Health).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(health, null);

                ApplyHit(target, child, network.LocalClientId, InventoryItemEnum.IronSword, 10);
                await UniTask.DelayFrame(2);

                Require(health.Network.Value == 90 && DamageMeterState.TotalDamage == 10,
                    "An accepted solo hit must traverse Health, child Player lookup and the actual owner ClientRpc.");
                Require(DamageMeterState.Entries.Count == 1 && DamageMeterState.Entries[0].CharacterId == 21
                    && DamageMeterState.Entries[0].Nickname == "Solo probe",
                    "Delivered damage must use the authoritative character identity.");

                ApplyHit(target, child, network.LocalClientId, InventoryItemEnum.IronBow, 20);
                ApplyHit(target, child, network.LocalClientId, InventoryItemEnum.IronSword, 500);
                await UniTask.DelayFrame(2);

                Require(DamageMeterState.TotalDamage == 100 && DamageMeterState.Entries.Count == 2
                    && DamageMeterState.Entries[0].WeaponType == InventoryItemEnum.IronSword
                    && DamageMeterState.Entries[0].Damage == 80,
                    "Health delivery must group weapons and clamp lethal damage to remaining HP.");

                ApplyHit(target, child, network.LocalClientId, InventoryItemEnum.IronSword, 10);
                await UniTask.DelayFrame(2);

                Require(DamageMeterState.TotalDamage == 100, "A dead target must not generate further meter records.");

                playerNetwork.Despawn();
                targetNetwork.Despawn();

                Require(DamageMeterState.TotalDamage == 0, "Owner despawn must clear the delivered meter history.");

                Debug.Log("DAMAGE_METER_SOLO_DELIVERY_VALIDATED: real NGO host, root/child PlayerObject routing, Health accepted hits, owner ClientRpc, authoritative identity, weapon grouping, overkill, dead target rejection and owner despawn reset. API authentication and remote-client transport are outside this probe.");

                network.Shutdown();
                await UniTask.WaitUntil(() => !network.IsListening).Timeout(TimeSpan.FromSeconds(10));

                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                network?.Shutdown();
                EditorApplication.Exit(1);
            }
        }

        private static void ApplyHit(GameObject target, GameObject player, ulong clientId, InventoryItemEnum weapon, float damage)
        {
            AttackTargetSubscription.Instance.Invoke(target.GetInstanceID().ToString(), new AttackTargetSubscriptionEvent
            {
                ClientId = clientId,
                Player = player,
                PlayerSessionId = "damage-meter-probe",
                WeaponType = weapon,
                Value = damage
            });
        }

        private static void ValidateAccumulator()
        {
            var changes = 0;
            Action changed = () => changes++;
            DamageMeterState.Changed += changed;

            try
            {
                DamageMeterState.Reset();
                DamageMeterState.Record(21, "First", InventoryItemEnum.IronSword, 10);
                DamageMeterState.Record(22, "Second", InventoryItemEnum.IronSword, 20);
                DamageMeterState.Record(21, "Renamed", InventoryItemEnum.IronSword, 10);
                DamageMeterState.Record(21, "Renamed", InventoryItemEnum.None, 5);

                Require(DamageMeterState.Entries.Count == 3, "Rows must group by character and weapon.");
                Require(DamageMeterState.TotalDamage == 45, "Total must sum every accepted record.");
                Require(DamageMeterState.Entries[0].CharacterId == 21, "Equal totals must preserve first-seen order.");
                Require(DamageMeterState.Entries[0].Nickname == "Renamed", "Latest authoritative nickname must be shown.");
                Require(DamageMeterState.Entries[0].Damage == 20, "Repeated hits must accumulate.");
                Require(changes == 5, "Each mutation must notify listeners once.");

                DamageMeterState.Record(22, "Second", InventoryItemEnum.IronSword, 0);
                DamageMeterState.Record(22, "Second", InventoryItemEnum.IronSword, -1);
                DamageMeterState.Record(22, "Second", InventoryItemEnum.IronSword, float.NaN);
                DamageMeterState.Record(22, "Second", InventoryItemEnum.IronSword, float.PositiveInfinity);
                DamageMeterState.Record(0, "Invalid", InventoryItemEnum.None, 100);

                Require(DamageMeterState.TotalDamage == 45 && changes == 5, "Invalid input must not affect totals or notifications.");

                DamageMeterState.Reset();

                Require(DamageMeterState.TotalDamage == 0 && DamageMeterState.Entries.Count == 0, "Reset must clear all historical rows.");
            }
            finally
            {
                DamageMeterState.Changed -= changed;
                DamageMeterState.Reset();
            }
        }

        private static void ValidateEffectiveDamage()
        {
            var calculate = typeof(Health).GetMethod("CalculateEffectiveDamage", BindingFlags.Static | BindingFlags.NonPublic);

            Require((float)calculate.Invoke(null, new object[] { 8f, 20f }) == 8f, "Overkill must count remaining HP only.");
            Require((float)calculate.Invoke(null, new object[] { 20f, 8f }) == 8f, "Nonlethal damage must retain its value.");
            Require((float)calculate.Invoke(null, new object[] { 8f, 8f }) == 8f, "Exact lethal damage must retain its value.");
        }

        private static void ValidatePartyRouting()
        {
            const ulong first = ulong.MaxValue - 101;
            const ulong second = ulong.MaxValue - 102;
            var party = typeof(Player).Assembly.GetType("Assets.Scripts.Areas.Party.Mono.PartyServerState", true);
            var recipients = typeof(Player).GetMethod("GetDamageMeterRecipients", BindingFlags.Static | BindingFlags.NonPublic);

            try
            {
                var solo = (IReadOnlyList<ulong>)recipients.Invoke(null, new object[] { first });
                Require(solo.Count == 1 && solo[0] == first, "Solo damage must route to attacker only.");

                party.GetMethod("Invite").Invoke(null, new object[] { first, second });
                party.GetMethod("Respond").Invoke(null, new object[] { second, first, true });

                var grouped = (IReadOnlyList<ulong>)recipients.Invoke(null, new object[] { second });
                Require(grouped.Count == 2 && Contains(grouped, first) && Contains(grouped, second),
                    "Damage must reach all current party members without reward proximity requirements.");

                DamageMeterState.Record(22, "Former member", InventoryItemEnum.IronSword, 10);
                party.GetMethod("Leave").Invoke(null, new object[] { second });

                var left = (IReadOnlyList<ulong>)recipients.Invoke(null, new object[] { second });
                Require(left.Count == 1 && left[0] == second, "Party departure must change subsequent routing immediately.");
                Require(DamageMeterState.Entries.Count == 1, "Party changes must retain historical rows until reset.");
            }
            finally
            {
                party.GetMethod("RemovePlayer").Invoke(null, new object[] { first });
                party.GetMethod("RemovePlayer").Invoke(null, new object[] { second });
                DamageMeterState.Reset();
            }
        }

        private static bool Contains(IReadOnlyList<ulong> members, ulong clientId)
        {
            foreach (var member in members)
            {
                if (member == clientId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
