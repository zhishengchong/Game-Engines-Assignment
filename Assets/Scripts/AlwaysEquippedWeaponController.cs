using UnityEngine;
using UnityEngine.InputSystem;

public class AlwaysEquippedWeaponController : MonoBehaviour
{
    [Header("永久持枪设置")]
    [Tooltip("把 WeaponHolder 下面的 Shotgun final 拖到这里")]
    public PlayerWeapon equippedWeapon;

    [Tooltip("把 MainCamera 下面的 WeaponHolder 拖到这里")]
    public Transform weaponHolder;

    [Header("持枪姿势微调")]
    public Vector3 weaponLocalPosition = Vector3.zero;
    public Vector3 weaponLocalEulerAngles = Vector3.zero;
    public Vector3 weaponLocalScale = new Vector3(0.3f, 0.3f, 0.3f);

    [Header("输入设置")]
    public bool allowShooting = true;

    private int _weaponLayer;

    void Start()
    {
        _weaponLayer = LayerMask.NameToLayer("Weapon");

        if (weaponHolder == null)
        {
            Transform foundHolder = transform.Find("MainCamera/WeaponHolder");

            if (foundHolder == null && Camera.main != null)
            {
                foundHolder = Camera.main.transform.Find("WeaponHolder");
            }

            weaponHolder = foundHolder;
        }

        if (equippedWeapon == null)
        {
            equippedWeapon = GetComponentInChildren<PlayerWeapon>(true);

            if (equippedWeapon == null && weaponHolder != null)
            {
                equippedWeapon = weaponHolder.GetComponentInChildren<PlayerWeapon>(true);
            }
        }

        if (equippedWeapon == null)
        {
            Debug.LogWarning("⚠️ [AlwaysEquippedWeaponController] 没有找到 PlayerWeapon。请把 WeaponHolder 下的 Shotgun final 拖到 Equipped Weapon。");
            return;
        }

        if (weaponHolder == null)
        {
            Debug.LogWarning("⚠️ [AlwaysEquippedWeaponController] 没有找到 WeaponHolder。请手动拖拽 WeaponHolder。");
            return;
        }

        SetupPermanentWeapon();
    }

    void LateUpdate()
    {
        if (equippedWeapon == null || weaponHolder == null) return;

        // 永久锁定枪的位置，防止它掉到地底/继承错误坐标
        equippedWeapon.transform.SetParent(weaponHolder);
        equippedWeapon.transform.localPosition = weaponLocalPosition;
        equippedWeapon.transform.localRotation = Quaternion.Euler(weaponLocalEulerAngles);
        equippedWeapon.transform.localScale = weaponLocalScale;
    }

    void Update()
    {
        if (!allowShooting) return;
        if (equippedWeapon == null) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            equippedWeapon.TryShoot();
        }
    }

    private void SetupPermanentWeapon()
    {
        equippedWeapon.gameObject.SetActive(true);

        equippedWeapon.transform.SetParent(weaponHolder);
        equippedWeapon.transform.localPosition = weaponLocalPosition;
        equippedWeapon.transform.localRotation = Quaternion.Euler(weaponLocalEulerAngles);
        equippedWeapon.transform.localScale = weaponLocalScale;

        Rigidbody rb = equippedWeapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider[] colliders = equippedWeapon.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        if (_weaponLayer >= 0)
        {
            SetLayerRecursively(equippedWeapon.gameObject, _weaponLayer);
        }

        Debug.Log("✅ [AlwaysEquippedWeaponController] 永久持枪系统已启动，枪械已锁定到 WeaponHolder。");
    }

    public bool HasWeapon()
    {
        return equippedWeapon != null;
    }

    public PlayerWeapon GetWeapon()
    {
        return equippedWeapon;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null || layer < 0) return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}