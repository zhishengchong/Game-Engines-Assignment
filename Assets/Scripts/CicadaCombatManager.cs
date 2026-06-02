using UnityEngine;
using UnityEngine.InputSystem; // Required for the new system
using System.Collections;

public enum TeleportType { Forced, Selectable, Forbidden }

public class CicadaCombatManager : MonoBehaviour
{
    [Header("Resources")]
    public int maxCharges = 3;
    public int currentCharges;

    [Header("Timings")]
    public float decisionWindow = 1.5f;
    public float stunDuration = 2.0f;

    [Header("Multipliers")]
    public float stunSpeedPenalty = 0.2f;

    private PlayerMovement movementScript;

    void Awake()
    {
        currentCharges = maxCharges;
        movementScript = GetComponent<PlayerMovement>();
    }

    public void OnEnemyKilled(Enemy enemy)
    {
        switch (enemy.type)
        {
            case TeleportType.Forced:
                HandleForcedTeleport(enemy.transform.position);
                break;

            case TeleportType.Selectable:
                if (currentCharges > 0)
                    StartCoroutine(SelectableDecisionRoutine(enemy.transform.position));
                break;

            case TeleportType.Forbidden:
                GainCharge(1);
                break;
        }
    }

    private void HandleForcedTeleport(Vector3 targetPos)
    {
        if (currentCharges > 0)
        {
            currentCharges--;
            ExecuteTeleport(targetPos);
        }
        else
        {
            ExecuteTeleport(targetPos);
            StartCoroutine(SoftStunRoutine());
        }
    }

    private IEnumerator SelectableDecisionRoutine(Vector3 targetPos)
    {
        float preDecisionTimeScale = Time.timeScale;
        Time.timeScale = 0.05f;

        float timer = 0;
        bool choiceMade = false;

        while (timer < decisionWindow)
        {
            timer += Time.unscaledDeltaTime;

            // NEW INPUT SYSTEM: Checking for Right Click or 'E' key
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
            {
                currentCharges--;
                ExecuteTeleport(targetPos);
                choiceMade = true;
                break;
            }
            yield return null;
        }

        if (!choiceMade) Time.timeScale = preDecisionTimeScale;
    }

    private IEnumerator SoftStunRoutine()
    {
        movementScript.SetStunModifier(stunSpeedPenalty);
        Debug.Log("SOFT STUN: Movement Malfunction");

        // Wait using real-time seconds because time might be slowed
        yield return new WaitForSecondsRealtime(stunDuration);

        movementScript.SetStunModifier(1.0f);
    }

    private void ExecuteTeleport(Vector3 target)
    {
        transform.position = target;
        Time.timeScale = 1.0f; // Snaps time back to normal flow on successful teleport
    }

    public void GainCharge(int amount)
    {
        currentCharges = Mathf.Clamp(currentCharges + amount, 0, maxCharges);
    }
}