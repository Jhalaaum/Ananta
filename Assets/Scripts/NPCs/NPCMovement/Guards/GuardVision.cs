using UnityEngine;

public class GuardVision : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewDistance = 10f;
    public float viewAngle = 90f;

    [Header("Layer Settings")]
    public LayerMask obstacleMask;

    private Transform player;

    public bool CanSeePlayer { get; private set; }
    public Vector3 LastKnownPlayerPosition { get; private set; }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void Update()
    {
        CanSeePlayer = CheckCanSeePlayer();
    }

    bool CheckCanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // 1. Distance check
        if (distanceToPlayer > viewDistance)
        {
            return false;
        }

        // 2. Angle check
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer > viewAngle / 2f)
        {
            return false;
        }

        // 3. Wall / obstacle check
        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer.normalized, distanceToPlayer, obstacleMask))
        {
            return false;
        }

        LastKnownPlayerPosition = player.position;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + leftBoundary * viewDistance);
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + rightBoundary * viewDistance);
    }
}