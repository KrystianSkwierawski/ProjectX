using System;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

public class Fishing : NetworkBehaviour
{
    [SerializeField] private GameObject _baitPrefab;

    [Header("Casting")]
    [SerializeField] private float _maxDistance = 4f;
    [SerializeField] private float _baitSurfaceOffset = 0f;
    [SerializeField] private float _preferredCastDistance = 8f;
    [SerializeField] private float _minCastDistance = 4f;
    [SerializeField] private float _maxCastDistance = 16f;
    [SerializeField] private float _castDistanceStep = 1f;
    [SerializeField] private float _maxCastAngleDegrees = 35f;
    [SerializeField] private float _verticalRaycastHeight = 5f;
    [SerializeField] private float _verticalRaycastDepth = 10f;

    [Header("Line Renderer")]
    [SerializeField] private float _lineWidth = 0.01f;
    [SerializeField] private float _sagAmount = 0.05f;
    [SerializeField] private int _lineSegments = 20;

    private LineRenderer _line;
    private Transform _tip;
    private GameObject _fishingRod;
    private GameObject[] _waters;
    private bool _isCasting = false;
    private float _castTime = 30f;
    private float _castTimer = 0f;

    private bool _isInterrupted = false;
    private float _interruptDuration = 0.2f;
    private float _interruptTimer = 0f;

    private Color _originalBarColor;
    private StarterAssetsInputs _input;

    private float _fishBrokeOffTimer = 0f;
    private float _fishBrokeOffTime = 3f;

    private GameObject _bait;
    private ObjectPool<GameObject> _pool;

    private readonly NetworkVariable<bool> _active =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<bool> _canFishOut =
       new NetworkVariable<bool>(
           false,
           NetworkVariableReadPermission.Owner,
           NetworkVariableWritePermission.Server
       );

    private void Awake()
    {
        _fishingRod = transform.Find("FishingRod").gameObject;
        _line = _fishingRod.GetComponent<LineRenderer>();
        _tip = _fishingRod.transform.Find("Tip");
    }

    public override void OnNetworkSpawn()
    {
        SetFishingRodActive(_active.Value);
        _active.OnValueChanged += OnRodActiveChanged;
        base.OnNetworkSpawn();
    }

    private void Start()
    {
        if (IsOwner)
        {
            _input = GetComponent<StarterAssetsInputs>();
            _waters = GameObject.FindGameObjectsWithTag("Water");
        }

        if (IsServer)
        {
            _pool = new ObjectPool<GameObject>(
               createFunc: () => Instantiate(_baitPrefab),
               actionOnRelease: (GameObject gameObject) => gameObject.GetComponent<NetworkObject>().Despawn(false)
            );
        }
    }

    [ServerRpc]
    private void CheckLootServerRpc(string clientToken)
    {
        _canFishOut.Value = false;

        // TODO: validation
        CheckLootSubscription.Instance.Invoke(OwnerClientId.ToString(), new CheckLootSubscriptionEvent
        {
            GameObjectName = nameof(CharacterInventoryTypeEnum.Fish)
        });

        UnityWebRequestHelper.ExecutePostAsync<AddCharacterExperienceDto>("CharacterExperiences", new AddCharacterExperienceCommand
        {
            characterId = 1,
            amount = 50,
            type = ExperienceTypeEnum.Fishing
        }, clientToken).Forget();
    }

    private void Update()
    {
        if (IsOwner)
        {
            CheckFishOut();
            CheckInterrupt();
            CheckInput();
            CheckCasting();
        }

        if (IsServer)
        {
            CheckCanFishOut();
            CheckFishBrokeOff();
        }
    }

    private void LateUpdate()
    {
        if (_bait != null)
        {
            DrawSagLine();
        }
    }

    private void CheckCanFishOut()
    {
        if (_canFishOut.Value || !_active.Value)
        {
            return;
        }

        float perSecondProb = 0.10f;
        float chance = 1f - Mathf.Pow(1f - perSecondProb, Time.deltaTime);

        if (UnityEngine.Random.value < chance)
        {
            Debug.Log($"Can fish out. OwnerClientId: {OwnerClientId}");

            _fishBrokeOffTime = UnityEngine.Random.Range(2f, 5f);
            _canFishOut.Value = true;

            SimulateBaitBiteAsync().Forget();

            NotifyCanFishOutClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            });
        }
    }

    private void CheckFishBrokeOff()
    {
        if (!_canFishOut.Value)
        {
            _fishBrokeOffTimer = 0f;
            return;
        }

        _fishBrokeOffTimer += Time.deltaTime;

        if (_fishBrokeOffTimer >= _fishBrokeOffTime)
        {
            _fishBrokeOffTimer = 0;
            _canFishOut.Value = false;

            NotifyFishBrokeOffClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            });
        }
    }

    [ClientRpc]
    private void NotifyCanFishOutClientRpc(ClientRpcParams rpcParams = default)
    {
        AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.FishReelIn);
    }

    [ClientRpc]
    private void NotifyFishBrokeOffClientRpc(ClientRpcParams rpcParams = default)
    {
        StopCasting();
    }

    private async UniTask SimulateBaitBiteAsync()
    {
        var baitTransform = _bait.transform;
        var startPos = baitTransform.position;

        float downAmount = UnityEngine.Random.Range(0.03f, 0.12f);
        Vector3 downPos = startPos + Vector3.down * downAmount;

        // Smooth sink duration (longer => smoother)
        float sinkDuration = UnityEngine.Random.Range(0.5f, 0.9f);
        float elapsed = 0f;
        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sinkDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            baitTransform.position = Vector3.Lerp(startPos, downPos, smooth);
            await UniTask.Yield();
        }

        // short pause at the bottom so movement is noticeable
        await UniTask.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(0.05f, 0.15f)));

        // Smooth return to start position
        float returnDuration = UnityEngine.Random.Range(0.35f, 0.55f);
        elapsed = 0f;
        Vector3 fromPos = baitTransform.position;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            baitTransform.position = Vector3.Lerp(fromPos, startPos, smooth);
            await UniTask.Yield();
        }

        // small, quick bob at the end to make the return feel natural
        float bobTime = UnityEngine.Random.Range(0.18f, 0.35f);
        float bobElapsed = 0f;
        float bobFreq = UnityEngine.Random.Range(6.0f, 9.0f); // quick, small bounce
        float bobAmp = Mathf.Clamp(downAmount * 0.06f, 0.002f, 0.008f);
        while (bobElapsed < bobTime)
        {
            bobElapsed += Time.deltaTime;
            float bob = Mathf.Sin(bobElapsed * bobFreq) * bobAmp;
            baitTransform.position = startPos + Vector3.up * bob;
            await UniTask.Yield();
        }

        baitTransform.position = startPos;
    }

    private void CheckFishOut()
    {
        var mouse = Mouse.current;

        if (!_canFishOut.Value || !_isCasting)
        {
            CursorUI.Instance.ShowDefault();

            return;
        }

        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

        var hover = Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "Bait" && hit.transform.gameObject.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId;

        if (!hover)
        {
            CursorUI.Instance.ShowDefault();

            return;
        }

        CursorUI.Instance.ShowPointer();

        if (mouse.rightButton.wasPressedThisFrame)
        {
            StopCasting();
            CheckLootServerRpc(UserManager.Instance.Token);
        }
    }

    private void CheckInput()
    {
        if (!_isCasting && !_isInterrupted && _input.Move == Vector2.zero && !_input.Jump && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (!TryGetNearestWater(out var water, out var waterCollider))
            {
                Debug.Log("Not near water");
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.CastingFailed, 0.1f);
                return;
            }

            if (!TryFindSpawnPointInWater(transform.position, ClampAimAngle(_maxCastAngleDegrees), water, waterCollider, out var spawnPos))
            {
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.CastingFailed, 0.1f);
                Debug.Log("Not found spawn point in water");
                return;
            }

            SpawnBaitServerRpc(spawnPos);

            _originalBarColor = PlayerUI.Instance.CastProgressBar.color;
            _isCasting = true;
            _castTimer = _castTime;
            PlayerUI.Instance.ShowCastBar(_castTimer / _castTime);

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.FishCast, 0.5f);
        }
    }

    [ServerRpc]
    private void SpawnBaitServerRpc(Vector3 spawnPos)
    {
        // TODO: validation
        _bait = _pool.Get();
        _bait.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
        var networkObject = _bait.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(OwnerClientId);

        _active.Value = true;

        NotifyBaitSpawnedClientRpc((NetworkObjectReference)networkObject);
    }

    [ServerRpc]
    private void DespawnServerRpc()
    {
        NotifyBaitDespawnedClientRpc();

        _pool.Release(_bait);
        _active.Value = false;
        _canFishOut.Value = false;
    }

    private void CheckCasting()
    {
        if (!_isCasting)
        {
            return;
        }

        if (_input.Move != Vector2.zero || _input.Jump)
        {
            InterruptCast();
            return;
        }

        _castTimer -= Time.deltaTime;
        var normalized = _castTime > 0f ? (_castTimer / _castTime) : 0f;
        PlayerUI.Instance.ShowCastBar(Mathf.Clamp01(normalized));

        if (_castTimer <= 0f)
        {
            StopCasting();
        }
    }

    private void StopCasting()
    {
        _isCasting = false;
        _castTimer = 0f;

        PlayerUI.Instance.HideCastBar();
        AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.FishingBobber, 1f);

        DespawnServerRpc();
    }

    private void InterruptCast()
    {
        _isCasting = false;
        _isInterrupted = true;
        _interruptTimer = 0f;

        PlayerUI.Instance.FailCastBar();
        AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.CastingFailed, 0.1f);

        DespawnServerRpc();
    }

    private void CheckInterrupt()
    {
        if (!_isInterrupted)
        {
            return;
        }

        _interruptTimer += Time.deltaTime;

        if (_interruptTimer >= _interruptDuration)
        {
            _isInterrupted = false;
            _interruptTimer = 0f;
            PlayerUI.Instance.HideCastBar();
            PlayerUI.Instance.CastProgressBar.color = _originalBarColor;
        }
    }

    private bool TryGetNearestWater(out GameObject nearest, out Collider nearestCollider)
    {
        nearest = null;
        nearestCollider = null;

        float bestScore = float.MaxValue;

        foreach (var water in _waters)
        {
            var col = water.GetComponent<Collider>();
            Vector3 closestPoint = (col != null) ? col.ClosestPoint(transform.position) : water.transform.position;
            float dist = Vector3.Distance(closestPoint, transform.position);

            if (dist > _maxDistance)
            {
                continue;
            }

            float yScore = (col != null) ? col.bounds.min.y : water.transform.position.y;
            float score = dist + (yScore * 0.001f);

            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            nearest = water;
            nearestCollider = col;
        }

        return nearest != null && nearestCollider != null;
    }

    private Vector3 ClampAimAngle(float maxDegrees)
    {
        var aim = transform.forward.normalized;
        var horizontal = Vector3.ProjectOnPlane(aim, Vector3.up);
        float horizMag = horizontal.magnitude;

        if (horizMag < 1e-6f)
        {
            horizontal = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            if (horizontal.magnitude < 1e-6f)
            {
                horizontal = Vector3.forward;
            }

            horizMag = horizontal.magnitude;
        }

        float angleDeg = Mathf.Abs(Mathf.Atan2(aim.y, horizMag) * Mathf.Rad2Deg);

        if (angleDeg <= maxDegrees)
        {
            return aim;
        }

        float sign = Mathf.Sign(aim.y);
        float constrainedRad = maxDegrees * Mathf.Deg2Rad;

        var newDir = (horizontal.normalized * Mathf.Cos(constrainedRad)) +
                     (Vector3.up * Mathf.Sin(constrainedRad) * sign);

        return newDir.normalized;
    }

    private bool TryFindSpawnPointInWater(Vector3 origin, Vector3 aimDir, GameObject waterObject, Collider waterCollider, out Vector3 spawn)
    {
        spawn = Vector3.zero;

        float start = Mathf.Clamp(_preferredCastDistance, _minCastDistance, _maxCastDistance);
        var bounds = waterCollider.bounds;

        Debug.Log($"TryFindSpawnPointInWater: bounds.min.y={bounds.min.y:F3}, bounds.max.y={bounds.max.y:F3}, startD={start}, maxD={_maxCastDistance}");

        for (float d = start; d <= _maxCastDistance; d += _castDistanceStep)
        {
            var candidate = origin + aimDir * d;
            var rayStart = candidate + Vector3.up * _verticalRaycastHeight;
            var rayLength = _verticalRaycastHeight + _verticalRaycastDepth;
            var downRay = new Ray(rayStart, Vector3.down);

            if (waterCollider.Raycast(downRay, out RaycastHit waterHit, rayLength))
            {
                bool insideXZ = IsPointInsideBoundsHorizontalXZ(waterHit.point, bounds);

                bool withinY = waterHit.point.y <= bounds.max.y + 0.15f &&
                               waterHit.point.y >= bounds.min.y - 0.15f;

                Debug.Log($"TryFindSpawnPointInWater: d={d:F2} candidate={candidate} -> waterCollider.Raycast HIT point={waterHit.point} insideXZ={insideXZ} withinY={withinY}");

                if (insideXZ && withinY)
                {
                    float y = Mathf.Min(waterHit.point.y, bounds.max.y) + _baitSurfaceOffset;
                    spawn = new Vector3(waterHit.point.x, y, waterHit.point.z);
                    Debug.Log($"TryFindSpawnPointInWater: accepted spawn={spawn} (waterCollider.Raycast)");

                    return true;
                }

                continue;
            }
        }

        if (Physics.SphereCast(origin + Vector3.up * 0.5f, 0.25f, aimDir, out RaycastHit sphereHit, _maxCastDistance) && IsColliderPartOfWater(sphereHit.collider, waterObject.transform))
        {
            var pt = sphereHit.point;
            float y = Mathf.Min(pt.y, bounds.max.y) + _baitSurfaceOffset;
            spawn = new Vector3(pt.x, y, pt.z);

            return true;
        }

        Debug.Log("TryFindSpawnPointInWater: no valid spawn found");

        return false;
    }

    private bool IsColliderPartOfWater(Collider collider, Transform waterTransform)
    {
        if (collider == null || waterTransform == null)
        {
            return false;
        }

        var transform = collider.transform;

        while (transform != null)
        {
            if (transform == waterTransform)
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private void SetFishingRodActive(bool value)
    {
        _fishingRod.SetActive(value);
    }

    private void OnRodActiveChanged(bool prev, bool next)
    {
        SetFishingRodActive(next);

        if (IsOwner)
        {
            ToggleLine(next);
        }
    }

    private bool IsPointInsideBoundsHorizontalXZ(Vector3 point, Bounds bounds)
    {
        return point.x >= bounds.min.x - 0.01f && point.x <= bounds.max.x + 0.01f
            && point.z >= bounds.min.z - 0.01f && point.z <= bounds.max.z + 0.01f;
    }

    public override void OnNetworkDespawn()
    {
        _active.OnValueChanged -= OnRodActiveChanged;
        base.OnNetworkDespawn();
    }

    [ClientRpc]
    private void NotifyBaitSpawnedClientRpc(NetworkObjectReference networkObjectRef)
    {
        if (networkObjectRef.TryGet(out var networkObject))
        {
            _bait = networkObject.gameObject;
            ToggleLine(true);
        }
    }

    [ClientRpc]
    private void NotifyBaitDespawnedClientRpc()
    {
        ToggleLine(false);
        _bait = null;
    }

    private void DrawSagLine()
    {
        var tipPosition = _tip.position;
        var baitPosition = _bait.transform.position;
        var segments = _lineSegments;

        segments = Mathf.Max(2, segments);

        if (_line.positionCount != segments + 1)
        {
            _line.positionCount = segments + 1;
        }

        float dist = Vector3.Distance(tipPosition, baitPosition);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(tipPosition, baitPosition, t);

            float parabola = 1f - Mathf.Pow(2f * t - 1f, 2f);
            p += Vector3.down * (_sagAmount * dist * parabola);

            _line.SetPosition(i, p);
        }
    }

    private void ToggleLine(bool enable)
    {
        if (enable)
        {
            _line.widthMultiplier = _lineWidth;

            var tipPosition = _tip.position;

            _line.positionCount = _lineSegments + 1;

            for (int i = 0; i <= _lineSegments; i++)
            {
                _line.SetPosition(i, tipPosition);
            }

            return;
        }

        _line.positionCount = 0;
    }
}