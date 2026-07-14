using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class CombatPlatform : MonoBehaviour
{
    private readonly HashSet<Collider> _playerColliders =
        new HashSet<Collider>();

    private Collider _zoneCollider;
    private Transform _player;

    public bool IsPlayerInside
    {
        get
        {
            RemoveInvalidPlayerColliders();
            return _playerColliders.Count > 0;
        }
    }

    public Transform Player => _player;

    public Collider ZoneCollider => _zoneCollider;

    private void Awake()
    {
        _zoneCollider = GetComponent<Collider>();

        if (_zoneCollider == null)
        {
            Debug.LogError(
                $"[CombatPlatform] {gameObject.name} 缺少 Collider。"
            );

            enabled = false;
            return;
        }

        if (!_zoneCollider.isTrigger)
        {
            Debug.LogWarning(
                $"[CombatPlatform] {gameObject.name} 的 Collider " +
                "没有勾选 Is Trigger，现已自动开启。"
            );

            _zoneCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterPlayerCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        RegisterPlayerCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_playerColliders.Remove(other))
        {
            RemoveInvalidPlayerColliders();

            if (_playerColliders.Count == 0)
            {
                _player = null;
            }
        }
    }

    private void RegisterPlayerCollider(Collider other)
    {
        Transform playerRoot = ResolvePlayerRoot(other);

        if (playerRoot == null)
        {
            return;
        }

        _playerColliders.Add(other);
        _player = playerRoot;
    }

    private Transform ResolvePlayerRoot(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        if (other.CompareTag("Player"))
        {
            return other.transform;
        }

        Transform root = other.transform.root;

        if (root != null && root.CompareTag("Player"))
        {
            return root;
        }

        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            return playerHealth.transform;
        }

        return null;
    }

    private void RemoveInvalidPlayerColliders()
    {
        _playerColliders.RemoveWhere(
            colliderItem => colliderItem == null
        );

        if (_playerColliders.Count == 0)
        {
            _player = null;
        }
    }

    /// <summary>
    /// 判断一个世界坐标是否仍然处于这个 CombatArea 内。
    /// Spike 会使用它判断自己是否冲出了所属平台。
    /// </summary>
    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        if (_zoneCollider == null)
        {
            _zoneCollider = GetComponent<Collider>();
        }

        if (_zoneCollider == null)
        {
            return false;
        }

        // 当前项目主要使用 BoxCollider。
        // 使用本地坐标判断，可以正确支持物体的位置、旋转和缩放。
        if (_zoneCollider is BoxCollider boxCollider)
        {
            Vector3 localPoint =
                boxCollider.transform.InverseTransformPoint(worldPoint);

            Vector3 difference =
                localPoint - boxCollider.center;

            Vector3 halfSize =
                boxCollider.size * 0.5f;

            return
                Mathf.Abs(difference.x) <= halfSize.x &&
                Mathf.Abs(difference.y) <= halfSize.y &&
                Mathf.Abs(difference.z) <= halfSize.z;
        }

        // 非 BoxCollider 的备用判断。
        Vector3 closestPoint =
            _zoneCollider.ClosestPoint(worldPoint);

        return
            (closestPoint - worldPoint).sqrMagnitude <=
            0.0001f;
    }

    public bool ContainsTransform(Transform target)
    {
        return
            target != null &&
            ContainsWorldPoint(target.position);
    }

    private void OnDisable()
    {
        _playerColliders.Clear();
        _player = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }
#endif
}