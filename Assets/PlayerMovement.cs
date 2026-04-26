using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    [Header("Movement Settings")]
    public float speed = 12f;
    public float gravity = -9.81f; // Standard Earth gravity
    
    private Vector3 velocity; // Stores our current vertical speed

    void Update()
    {
        // 1. Reset velocity if we are grounded
        if (controller.isGrounded && velocity.y < 0)
        {
            // A small negative value ensures the player stays snapped to the floor
            velocity.y = -2f; 
        }

        // 2. Horizontal Movement
        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed) x = -1f;
        if (Keyboard.current.dKey.isPressed) x = 1f;
        if (Keyboard.current.wKey.isPressed) z = 1f;
        if (Keyboard.current.sKey.isPressed) z = -1f;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // 3. Vertical Movement (Gravity)
        // velocity = acceleration * time
        velocity.y += gravity * Time.deltaTime;

        // distance = velocity * time
        controller.Move(velocity * Time.deltaTime);
    }
}