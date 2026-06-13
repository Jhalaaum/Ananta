using System;
using UnityEngine;

public class GuardVision : MonoBehaviour
{
    [Header("Vision Shape")]
    public float viewDistance = 12f;
    [Range(1f, 180f)] public float viewAngle = 85f;
    public float eyeHeight = 1.65f;

    [Header("Target")]
    public string playerTag = "Player";
    public Transform player;
    public Transform eyePoint;

    [Header("Line Of Sight")]
    public LayerMask lineOfSightMask = ~0;
    public bool ignoreTriggers = true;
    public bool requireLineOfSight = true;

    [Header("Target Points")]
    public float targetChestHeight = 1.2f;
    public float targetHeadHeight = 1.65f;
    public float targetFeetHeight = 0.25f;
    public bool checkHead = true;
    public bool checkChest = true;
    public bool checkFeet = false;

    [Header("Vision Memory")]
    public float lastSeenMemoryDuration = 0.25f;

    [Header("Runtime")]
    public bool CanSeePlayer;
    public Vector3 LastKnownPlayerPosition;
    public Vector3 LastVisiblePoint;
    public float lastSeenTimer;
    public float currentVisibilityStrength;

    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool drawVisionGizmos = true;

    private Collider[] playerColliders;
    private Collider[] ownColliders;

    private void Awake()
    {
        if (eyePoint == null)
        {
            eyePoint = transform;
        }

        ownColliders = GetComponentsInChildren<Collider>();

        FindPlayerIfNeeded();
    }

    private void Update()
    {
        FindPlayerIfNeeded();
        UpdateVision();
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject == null)
        {
            return;
        }

        player = playerObject.transform;
        playerColliders = playerObject.GetComponentsInChildren<Collider>();
    }

    private void UpdateVision()
    {
        if (player == null)
        {
            CanSeePlayer = false;
            currentVisibilityStrength = 0f;
            return;
        }

        bool sawPlayerThisFrame = TrySeePlayer(out Vector3 visiblePoint, out float visibilityStrength);

        if (sawPlayerThisFrame)
        {
            CanSeePlayer = true;
            LastKnownPlayerPosition = player.position;
            LastVisiblePoint = visiblePoint;
            lastSeenTimer = lastSeenMemoryDuration;
            currentVisibilityStrength = visibilityStrength;
            return;
        }

        if (lastSeenTimer > 0f)
        {
            lastSeenTimer -= Time.deltaTime;
        }

        CanSeePlayer = false;
        currentVisibilityStrength = 0f;
    }

    private bool TrySeePlayer(out Vector3 visiblePoint, out float visibilityStrength)
    {
        visiblePoint = Vector3.zero;
        visibilityStrength = 0f;

        Vector3 eyePosition = GetEyePosition();

        Vector3[] targetPoints = BuildTargetPoints();

        foreach (Vector3 targetPoint in targetPoints)
        {
            Vector3 directionToTarget = targetPoint - eyePosition;
            float distanceToTarget = directionToTarget.magnitude;

            if (distanceToTarget > viewDistance)
            {
                continue;
            }

            if (distanceToTarget <= 0.01f)
            {
                continue;
            }

            Vector3 direction = directionToTarget.normalized;

            float angle = Vector3.Angle(transform.forward, direction);

            if (angle > viewAngle * 0.5f)
            {
                continue;
            }

            if (requireLineOfSight && IsBlockedByWall(eyePosition, direction, distanceToTarget))
            {
                continue;
            }

            visiblePoint = targetPoint;

            float distanceScore = 1f - Mathf.Clamp01(distanceToTarget / viewDistance);
            float angleScore = 1f - Mathf.Clamp01(angle / (viewAngle * 0.5f));

            visibilityStrength = Mathf.Clamp01((distanceScore * 0.55f) + (angleScore * 0.45f));

            return true;
        }

        return false;
    }

    private Vector3[] BuildTargetPoints()
    {
        Vector3 basePosition = player.position;

        if (checkHead && checkChest && checkFeet)
        {
            return new Vector3[]
            {
                basePosition + Vector3.up * targetHeadHeight,
                basePosition + Vector3.up * targetChestHeight,
                basePosition + Vector3.up * targetFeetHeight
            };
        }

        if (checkHead && checkChest)
        {
            return new Vector3[]
            {
                basePosition + Vector3.up * targetHeadHeight,
                basePosition + Vector3.up * targetChestHeight
            };
        }

        if (checkChest)
        {
            return new Vector3[]
            {
                basePosition + Vector3.up * targetChestHeight
            };
        }

        return new Vector3[]
        {
            basePosition + Vector3.up * targetHeadHeight
        };
    }

    private bool IsBlockedByWall(Vector3 origin, Vector3 direction, float distance)
    {
        QueryTriggerInteraction triggerMode = ignoreTriggers
            ? QueryTriggerInteraction.Ignore
            : QueryTriggerInteraction.Collide;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            distance,
            lineOfSightMask,
            triggerMode
        );

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (IsOwnCollider(hit.collider))
            {
                continue;
            }

            if (IsPlayerCollider(hit.collider))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private bool IsOwnCollider(Collider candidate)
    {
        if (candidate == null || ownColliders == null)
        {
            return false;
        }

        foreach (Collider ownCollider in ownColliders)
        {
            if (ownCollider == candidate)
            {
                return true;
            }
        }

        return candidate.transform.IsChildOf(transform);
    }

    private bool IsPlayerCollider(Collider candidate)
    {
        if (candidate == null || player == null)
        {
            return false;
        }

        if (candidate.transform == player || candidate.transform.IsChildOf(player))
        {
            return true;
        }

        if (playerColliders == null)
        {
            return false;
        }

        foreach (Collider playerCollider in playerColliders)
        {
            if (playerCollider == candidate)
            {
                return true;
            }
        }

        return false;
    }

    public Vector3 GetEyePosition()
    {
        if (eyePoint != null && eyePoint != transform)
        {
            return eyePoint.position;
        }

        return transform.position + Vector3.up * eyeHeight;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawVisionGizmos)
        {
            return;
        }

        Vector3 eyePosition = eyePoint != null
            ? eyePoint.position
            : transform.position + Vector3.up * eyeHeight;

        Gizmos.color = CanSeePlayer ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(eyePosition, 0.12f);

        Vector3 leftDirection = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 rightDirection = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;

        Gizmos.DrawLine(eyePosition, eyePosition + leftDirection * viewDistance);
        Gizmos.DrawLine(eyePosition, eyePosition + rightDirection * viewDistance);

        if (CanSeePlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(eyePosition, LastVisiblePoint);
            Gizmos.DrawWireSphere(LastVisiblePoint, 0.15f);
        }
    }
}