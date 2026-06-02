using UnityEngine;

public class Enemy : MonoBehaviour
{
    public TeleportType type;

    // Call this function from your weapon/damage script when enemy dies
    public void Die()
    {
        CicadaCombatManager playerCombat = FindObjectOfType<CicadaCombatManager>();
        if (playerCombat != null)
        {
            playerCombat.OnEnemyKilled(this);
        }
        Destroy(gameObject);
    }
}