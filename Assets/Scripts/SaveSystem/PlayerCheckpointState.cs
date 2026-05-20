using UnityEngine;

public class PlayerCheckpointState : MonoBehaviour
{
    [Header("References")]
    public CharacterController characterController;
    public Rigidbody playerRigidbody;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }
    }

    public void RestoreTransform(Vector3 position, Quaternion rotation)
    {
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

        transform.position = position;
        transform.rotation = rotation;

        Physics.SyncTransforms();

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // Keep Rigidbody kinematic if you are using CharacterController movement
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }
    }
}