using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleShooter : MonoBehaviour
{
    [Header("Settings")]
    public float range = 100f;
    public float fireRate = 0.15f; // Faster for Cicadamata feel
    private float nextFireTime = 0f;

    [Header("References")]
    public Camera playerCamera; // Drag your Main Camera here
    public ParticleSystem muzzleFlash;

    void Update()
    {
        // Debug line so you can see where you are aiming in the Scene View
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * range, Color.red);

        if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        if (muzzleFlash != null) muzzleFlash.Play();

        // Fire from the Camera's center point
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Use a LayerMask or ignore the Player so you don't shoot yourself
        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name);

            Enemy enemy = hit.transform.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die();
            }
        }
    }
}