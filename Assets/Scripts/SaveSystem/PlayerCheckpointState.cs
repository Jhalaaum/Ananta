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

        transform.SetPositionAndRotation(position, rotation);

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }
}