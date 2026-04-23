using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float baseMoveSpeed = 10f;
    private float currentStunMultiplier = 1.0f;

    public void SetStunModifier(float multiplier)
    {
        currentStunMultiplier = multiplier;
    }

    void Update()
    {
        // QUICK START: Reads WASD or Arrow Keys directly from the keyboard
        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) input.y = 1;
        if (Keyboard.current.sKey.isPressed) input.y = -1;
        if (Keyboard.current.aKey.isPressed) input.x = -1;
        if (Keyboard.current.dKey.isPressed) input.x = 1;

        // Calculate movement
        Vector3 move = (transform.right * input.x + transform.forward * input.y)
                       * baseMoveSpeed * currentStunMultiplier;

        transform.position += move * Time.deltaTime;
    }
}