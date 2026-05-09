using UnityEngine;

public class GuardVision : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewDistance = 12f;
    [Range(1f, 180f)] public float viewAngle = 90f;
    public float eyeHeight = 1.6f;

    [Header("Layer Settings")]
    public LayerMask obstacleMask;

    [Header("Debug")]
    public bool drawDebug = true;
    public Color canSeeColor = Color.red;
    public Color normalVisionColor = Color.yellow;

    private Transform player;

    public bool CanSeePlayer { get; private set; }
    public Vector3 LastKnownPlayerPosition { get; private set; }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        CanSeePlayer = CheckCanSeePlayer();
    }

    private bool CheckCanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = player.position + Vector3.up * 1.2f;

        Vector3 directionToPlayer = targetPosition - eyePosition;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > viewDistance)
        {
            return false;
        }

        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer > viewAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(
            eyePosition,
            directionToPlayer.normalized,
            distanceToPlayer,
            obstacleMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        LastKnownPlayerPosition = player.position;
        return true;
    }

    private void OnDrawGizmos()
    {
        if (!drawDebug)
        {
            return;
        }

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;

        Gizmos.color = CanSeePlayer ? canSeeColor : normalVisionColor;

        Vector3 leftBoundary = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;

        Gizmos.DrawLine(eyePosition, eyePosition + leftBoundary * viewDistance);
        Gizmos.DrawLine(eyePosition, eyePosition + rightBoundary * viewDistance);
        Gizmos.DrawWireSphere(eyePosition, viewDistance);

        int segments = 20;
        Vector3 previousPoint = eyePosition + leftBoundary * viewDistance;

        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-viewAngle * 0.5f, viewAngle * 0.5f, i / (float)segments);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Vector3 nextPoint = eyePosition + direction * viewDistance;

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}