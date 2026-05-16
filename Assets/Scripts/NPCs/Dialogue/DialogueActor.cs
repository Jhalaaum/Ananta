using UnityEngine;

public class DialogueActor : MonoBehaviour
{
    [Header("Identity")]
    public string actorID;
    public string displayName;

    [Header("Facing")]
    public bool faceTargetDuringDialogue = true;
    public float turnSpeed = 8f;

    private Transform currentLookTarget;

    public void LookAt(Transform target)
    {
        currentLookTarget = target;
    }

    public void StopLooking()
    {
        currentLookTarget = null;
    }

    private void Update()
    {
        if (!faceTargetDuringDialogue)
        {
            return;
        }

        if (currentLookTarget == null)
        {
            return;
        }

        Vector3 direction = currentLookTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * turnSpeed
        );
    }
}