using System;
using Assets.Scripts.Areas.Shared.Mono;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Areas.Hideout
{
    public sealed class HideoutBuilding : MonoBehaviour
    {
        [SerializeField] private HideoutBuildingEnum _buildingId;

        private GameObject _farm;
        private GameObject _station;
        private Spawner _spawner;
        private TMP_Text _countdown;
        private Material _ghost;

        private Renderer[] _renderers;
        private Material[][] _materials;
        private DateTimeOffset _apiTime;
        private double _receivedAt;
        private bool _complete;
        private bool _server;

        public HideoutBuildingDto Definition { get; private set; }

        public HideoutBuildingEnum BuildingId => _buildingId;

        public DateTimeOffset CurrentTime => _apiTime.AddSeconds(Time.realtimeSinceStartupAsDouble - _receivedAt);

        public bool CanBuild => Definition != null && !Definition.BuildEndsAt.HasValue;

        public bool IsInRange(Transform player) => Vector3.Distance(player.position, transform.position) <= DungeonPortal.InteractionDistance;

        private void Awake()
        {
            _farm = transform.Find("Farm").gameObject;
            _station = transform.Find("BuildStation").gameObject;
            _spawner = _farm.GetComponentInChildren<Spawner>(true);
            _countdown = transform.Find("Countdown").GetComponent<TMP_Text>();
            _ghost = Resources.Load<Material>("HideoutGhost");

            _renderers = _farm.GetComponentsInChildren<Renderer>(true);
            _materials = new Material[_renderers.Length][];

            for (var i = 0; i < _renderers.Length; i++)
            {
                _materials[i] = _renderers[i].sharedMaterials;
            }

            _station.GetComponent<Renderer>().sharedMaterial = _ghost;

            _spawner.gameObject.SetActive(false);
            _farm.SetActive(false);
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
            _farm.SetActive(!CanBuild);

            if (!CanBuild)
            {
                for (var i = 0; i < _renderers.Length; i++)
                {
                    var ghostMaterials = new Material[_materials[i].Length];
                    Array.Fill(ghostMaterials, _ghost);
                    _renderers[i].sharedMaterials = ghostMaterials;
                }
            }

            Update();
        }

        private void Update()
        {
            if (Definition?.BuildEndsAt == null || _complete)
            {
                return;
            }

            var now = CurrentTime;
            var remaining = (Definition.BuildEndsAt.Value - now).TotalSeconds;

            if (remaining > 0)
            {
                _countdown.gameObject.SetActive(!_server);
                _countdown.text = TimeSpan.FromSeconds(Math.Ceiling(remaining)).ToString(@"mm\:ss");

                if (Camera.main != null)
                {
                    _countdown.transform.rotation = Camera.main.transform.rotation;
                }

                return;
            }

            _complete = true;
            _countdown.gameObject.SetActive(false);

            for (var i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sharedMaterials = _materials[i];
            }

            _spawner.gameObject.SetActive(_server);
            Debug.Log($"Hideout building completed. Building: {Definition.Id}, Server: {_server}.");
        }
    }
}
