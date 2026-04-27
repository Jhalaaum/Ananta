using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    [Header("Walking Settings")]
    public float maxWalkingSpeed = 6f;

    [Header("Sprinting Settings")]
    public float maxSprintingSpeed = 20f;

    [Header("Acceleration Settings")]
    public float accelerationTime = 3.0f; // Time to reach max speed
    public float decelerationTime = 1.0f;

    [Header("General Settings")]
    public float gravity = -9.81f;

    private Vector3 velocity;
    private float currentSpeedFactor = 0f; // 0 → 1
    private float accelTimer = 0f;

    void Update()
    {
        // Ground check
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Input
        float x = 0f;
        float z = 0f;
        if (Keyboard.current.shiftKey.isPressed){
            if (Keyboard.current.aKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed) x = 1f;
            if (Keyboard.current.wKey.isPressed) z = 1f;
            if (Keyboard.current.sKey.isPressed) z = -1f;

            Vector3 move = transform.right * x + transform.forward * z;
            bool isMoving = move.magnitude > 0.1f;

            // Normalize direction
            if (isMoving) move.Normalize();

            // ACCELERATION (SINE CURVE)
            if (isMoving)
            {
                accelTimer += Time.deltaTime;
                float t = Mathf.Clamp01(accelTimer / accelerationTime);

                // Half sine curve (ease-in)
                currentSpeedFactor = Mathf.Sin(t * Mathf.PI * 0.5f);
            }
            else
            {
                accelTimer -= Time.deltaTime * (accelerationTime / decelerationTime);
                accelTimer = Mathf.Max(0f, accelTimer);

                float t = Mathf.Clamp01(accelTimer / accelerationTime);
                currentSpeedFactor = Mathf.Sin(t * Mathf.PI * 0.5f);
            }

            float currentSpeed = maxSprintingSpeed * currentSpeedFactor;

            controller.Move(move * currentSpeed * Time.deltaTime);


        } else{
            if (Keyboard.current.aKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed) x = 1f;
            if (Keyboard.current.wKey.isPressed) z = 1f;
            if (Keyboard.current.sKey.isPressed) z = -1f;

            Vector3 move = transform.right * x + transform.forward * z;
            bool isMoving = move.magnitude > 0.1f;

            // Normalize direction
            if (isMoving) move.Normalize();

            // ACCELERATION (SINE CURVE)
            if (isMoving)
            {
                accelTimer += Time.deltaTime;
                float t = Mathf.Clamp01(accelTimer / accelerationTime);

                // Half sine curve (ease-in)
                currentSpeedFactor = Mathf.Sin(t * Mathf.PI * 0.5f);
            }
            else
            {
                accelTimer -= Time.deltaTime * (accelerationTime / decelerationTime);
                accelTimer = Mathf.Max(0f, accelTimer);

                float t = Mathf.Clamp01(accelTimer / accelerationTime);
                currentSpeedFactor = Mathf.Sin(t * Mathf.PI * 0.5f);
            }

            float currentSpeed = maxWalkingSpeed * currentSpeedFactor;

            controller.Move(move * currentSpeed * Time.deltaTime);
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}