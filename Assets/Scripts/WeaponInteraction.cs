using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInteraction : MonoBehaviour
{
    [Header("拾取设置")]
    public float pickupRange = 3f;           
    public LayerMask weaponLayer;            
    public Transform weaponHolder;           

    [Header("丢掷设置")]
    public float throwForce = 20f;           
    public float throwSpin = 10f;            

    [Header("状态 (Debug观察用)")]
    [SerializeField] private GameObject equippedWeapon = null; 
    private Rigidbody _equippedRb;

    private Camera _mainCamera;
    private Collider _playerCollider;

    void Start()
    {
        _mainCamera = Camera.main;
        
        _playerCollider = GetComponent<Collider>();
        if (_playerCollider == null) 
        {
            _playerCollider = GetComponentInChildren<CharacterController>();
        }
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame && equippedWeapon == null)
        {
            TryPickupWeapon();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame && equippedWeapon != null)
        {
            ThrowWeapon();
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && equippedWeapon != null)
        {
            PlayerWeapon weaponScript = equippedWeapon.GetComponent<PlayerWeapon>();
            if (weaponScript != null)
            {
                weaponScript.TryShoot(); 
            }
        }

        if (equippedWeapon != null && weaponHolder != null)
        {
            equippedWeapon.transform.localPosition = Vector3.zero;
            equippedWeapon.transform.localRotation = Quaternion.identity;
        }
    }

    // ==========================================
    // 【新增】：供 TimeManager 查询当前手里是否有枪
    // ==========================================
    public bool HasWeapon()
    {
        return equippedWeapon != null;
    }

    private void TryPickupWeapon()
    {
        if (weaponHolder == null) return; 

        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange, weaponLayer))
        {
            _equippedRb = hit.collider.attachedRigidbody;
            
            if (_equippedRb != null)
            {
                equippedWeapon = _equippedRb.gameObject;
                _equippedRb.isKinematic = true;
                _equippedRb.interpolation = RigidbodyInterpolation.None;
            }
            else
            {
                equippedWeapon = hit.collider.gameObject;
            }

            Collider[] allColliders = equippedWeapon.GetComponentsInChildren<Collider>();
            foreach (Collider col in allColliders)
            {
                col.enabled = false;
            }

            equippedWeapon.transform.SetParent(weaponHolder);
            equippedWeapon.transform.localPosition = Vector3.zero;
            equippedWeapon.transform.localRotation = Quaternion.identity;
        }
    }

    private void ThrowWeapon()
    {
        equippedWeapon.transform.SetParent(null);

        Collider[] allColliders = equippedWeapon.GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            col.enabled = true;
            if (_playerCollider != null)
            {
                Physics.IgnoreCollision(_playerCollider, col, true);
            }
        }

        if (_equippedRb != null)
        {
            _equippedRb.isKinematic = false;
            _equippedRb.interpolation = RigidbodyInterpolation.Interpolate;
            
            Vector3 throwDirection = _mainCamera.transform.forward;
            _equippedRb.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);
            
            Vector3 randomSpin = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            _equippedRb.AddTorque(randomSpin * throwSpin, ForceMode.VelocityChange);
        }

        equippedWeapon = null;
        _equippedRb = null;
    }
}