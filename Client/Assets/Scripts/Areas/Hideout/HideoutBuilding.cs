using System;
using System.Linq;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Areas.Hideout
{
    public sealed class HideoutBuilding : MonoBehaviour
    {
        [Serializable]
        public sealed class CropDefinition
        {
            public InventoryItemEnum Seed;
            public InventoryItemEnum Product;
            public Spawner Spawner;
        }

        [SerializeField] private HideoutBuildingEnum _buildingId;
        [SerializeField] private CropDefinition[] _crops = Array.Empty<CropDefinition>();

        private GameObject _content;
        private GameObject _station;
        private TMP_Text _countdown;
        private Material _ghost;
        private Renderer[] _renderers;
        private Material[][] _materials;
        private DateTimeOffset _apiTime;
        private double _receivedAt;
        private bool _complete;
        private bool _server;
        [SerializeField] private Spawner[] _slots = new Spawner[6];
        private CharacterHideout _owner;
        private int _instanceId;
        private float _nextRefresh;

        public int InstanceId => _instanceId;

        public HideoutBuildingDto Definition { get; private set; }

        public HideoutBuildingEnum BuildingId => _buildingId;

        public DateTimeOffset CurrentTime => _apiTime.AddSeconds(Time.realtimeSinceStartupAsDouble - _receivedAt);

        public bool CanBuild => Definition != null && !Definition.BuildEndsAt.HasValue;

        public bool CanManage => Definition != null;

        public bool IsInRange(Transform player) => Vector3.Distance(player.position, transform.position) <= DungeonPortal.InteractionDistance;

        public bool IsSeed(InventoryItemEnum type) => _crops.Any(x => x.Seed == type);

        public double Interval(int slot, InventoryItemEnum seed = InventoryItemEnum.None)
        {
            var state = Definition?.Farm?.Slots;
            var type = seed != InventoryItemEnum.None ? seed : state != null && slot >= 0 && slot < state.Length ? state[slot].Seeds.Type : InventoryItemEnum.None;
            var crop = _crops
                .Where(x => x.Seed == type)
                .FirstOrDefault();

            return crop?.Spawner != null ? crop.Spawner.RespawnInterval : 5;
        }

        public void Bind(CharacterHideout owner, int instanceId)
        {
            _owner = owner;
            _instanceId = instanceId;
        }

        public int FindCrop(GameObject crop)
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].Crop == crop)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Awake()
        {
            _content = transform.Find("Content").gameObject;
            _station = transform.Find("BuildStation").gameObject;
            _countdown = transform.Find("Countdown").GetComponent<TMP_Text>();
            _ghost = Resources.Load<Material>("HideoutGhost");
            _renderers = _content.GetComponentsInChildren<Renderer>(true);
            _materials = _renderers.Select(x => x.sharedMaterials).ToArray();

            foreach (var spawner in _content.GetComponentsInChildren<Spawner>(true))
            {
                spawner.ControlPopulation();
                spawner.gameObject.SetActive(false);
            }

            _station.GetComponent<Renderer>().sharedMaterial = _ghost;
            _content.SetActive(false);
            _station.SetActive(false);
            _countdown.gameObject.SetActive(false);
        }

        public void Apply(HideoutBuildingDto definition, DateTimeOffset apiTime, bool server)
        {
            Definition = definition;
            _apiTime = apiTime;
            _receivedAt = Time.realtimeSinceStartupAsDouble;
            _server = server;
            _complete = false;
            _station.SetActive(CanBuild);
            _content.SetActive(!CanBuild);

            Update();

            if (_complete)
            {
                RestoreCrops();
            }
        }

        private void RestoreCrops()
        {
            var state = Definition.Farm;

            for (var i = 0; i < _slots.Length; i++)
            {
                var spawner = _slots[i];
                var slot = state != null && i < state.Capacity ? state.Slots[i] : null;
                var product = slot?.Ready ?? InventoryItemEnum.None;
                var seed = slot != null && slot.Seeds.Count > 0 ? slot.Seeds.Type : InventoryItemEnum.None;

                var definition = _crops
                    .Where(x => product != InventoryItemEnum.None ? x.Product == product : x.Seed == seed)
                    .SingleOrDefault();

                if (spawner == null)
                {
                    Debug.LogError($"Farm slot spawner missing. Building: {BuildingId}, Slot: {i}.");

                    continue;
                }

                spawner.ConfigureInstance(_instanceId);
                spawner.ConfigureCrop(definition?.Spawner?.Prefab);
                spawner.gameObject.SetActive(definition != null);
                spawner.ShowCrop(product != InventoryItemEnum.None && definition != null);
            }
        }

        private void Update()
        {
            if (Definition?.BuildEndsAt == null)
            {
                return;
            }

            var now = CurrentTime;
            var remaining = (Definition.BuildEndsAt.Value - now).TotalSeconds;
            var complete = remaining <= 0;

            if (complete != _complete || !_complete)
            {
                for (var i = 0; i < _renderers.Length; i++)
                {
                    _renderers[i].sharedMaterials = complete ? _materials[i] : Enumerable.Repeat(_ghost, _materials[i].Length).ToArray();
                }

                _complete = complete;
            }

            var upgrade = Definition.Farm?.UpgradeEndsAt;

            if (complete && upgrade.HasValue)
            {
                remaining = (upgrade.Value - now).TotalSeconds;
            }

            _countdown.gameObject.SetActive(!_server && remaining > 0);

            if (remaining > 0)
            {
                _countdown.text = TimeSpan.FromSeconds(Math.Ceiling(remaining)).ToString(@"hh\:mm\:ss");

                if (Camera.main != null)
                {
                    _countdown.transform.rotation = Camera.main.transform.rotation;
                }
            }

            var due = upgrade.HasValue && upgrade <= now || Definition.Farm?.Slots.Any(x => x.ReadyAt.HasValue && x.ReadyAt <= now) == true;

            if (_server && complete && due && _owner != null && Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 1;
                _owner.Refresh(this);
            }
        }
    }
}
