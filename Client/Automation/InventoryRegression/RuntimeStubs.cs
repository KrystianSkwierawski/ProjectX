// This harness compiles the real dedicated-server gear code. Only engine/UI and
// service boundaries are replaced; it does not validate NGO, HTTP or Play Mode.
using System;
using System.Collections.Generic;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Models;

namespace Unity.Netcode
{
    public interface IReaderWriter { }
    public interface INetworkSerializable
    {
        void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter;
    }
    public struct BufferSerializer<T> where T : IReaderWriter
    {
        public bool IsReader => false;
        public void SerializeValue<TValue>(ref TValue value) => throw new NotSupportedException();
    }
}

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }

    public sealed class SerializeField : Attribute { }
    public static class Mathf
    {
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
        public static int RoundToInt(float value) => (int)Math.Round(value);
    }
    public static class Random
    {
        public static float Range(float min, float max) => throw new NotSupportedException();
    }
}
namespace UnityEngine.TextCore.Text { public sealed class Unused { } }
namespace Cysharp.Threading.Tasks { public sealed class Unused { } }
namespace Assets.Scripts.Areas.Shared.Enums { public sealed class Unused { } }
namespace Assets.Scripts.Areas.Shared.Models { public sealed class Unused { } }
namespace Assets.Scripts.Areas.Shared.Mono { public sealed class Unused { } }
namespace Assets.Scripts.Areas.Shared.UI { public sealed class Unused { } }

namespace Assets.Scripts.Areas.Character
{
    public sealed class UserManager
    {
        public static UserManager Instance { get; } = new();
        public Dictionary<ulong, CharacterDto> Characters { get; } = new();
    }
}

namespace Assets.Scripts.Areas.Character.UI
{
    public sealed class GearSlot { }
    public sealed class GearUI
    {
        public static GearUI Instance => throw new InvalidOperationException("Server accessed gear UI");
        public GearSlot Helmet { get; }
        public GearSlot Chest { get; }
        public GearSlot Boots { get; }
        public GearSlot Weapon { get; }
        public GearSlot Ammo { get; }
        public void Wear(GearSlot slot, InventoryItemDto item) => throw new NotSupportedException();
        public void UpdateRightPanel() => throw new NotSupportedException();
    }
    public sealed class PlayerUI
    {
        public static PlayerUI Instance => throw new InvalidOperationException("Server accessed player UI");
        public void SetMaxHealth(int value) => throw new NotSupportedException();
    }
}

namespace Assets.Scripts.Areas.Shared.Subscriptions
{
    public abstract class AbstractSubscription<T, TEvent> where T : new()
    {
        public static T Instance { get; } = new();
        public TEvent LastEvent { get; private set; }
        public void Invoke(string key, TEvent value) => LastEvent = value;
    }
}

namespace Assets.Scripts.Areas.Party.Mono
{
    public static class Party
    {
        public static void NotifyCharacterChanged(ulong clientId) { }
    }
}
