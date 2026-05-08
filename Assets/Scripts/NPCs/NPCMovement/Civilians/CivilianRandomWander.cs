using UnityEngine;
using UnityEngine.AI;

public class CivilianRandomWander : MonoBehaviour
{
    [Header("Wander Area")]
    public bool useLimitedArea = true;
    public Transform wanderCenter;
    public float wanderRadius = 10f;

    [Header("Timing")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    [Header("Movement")]
    public float reachDistance = 0.7f;
    public int maxAttemptsToFindPoint = 10;

    private NavMeshAgent agent;
    private float waitTimer;
    private bool isWaiting;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (wanderCenter == null)
        {
            wanderCenter = transform;
        }

        PickNewDestination();
    }

    void Update()
    {
        if (agent.pathPending)
        {
            return;
        }

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickNewDestination();
            }

            return;
        }

        if (agent.remainingDistance <= reachDistance)
        {
            StartWaiting();
        }
    }

    void StartWaiting()
    {
        isWaiting = true;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    void PickNewDestination()
    {
        Vector3 center;

        if (useLimitedArea)
        {
            center = wanderCenter.position;
        }
        else
        {
            center = transform.position;
        }

        for (int i = 0; i < maxAttemptsToFindPoint; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;

            Vector3 randomPosition = new Vector3(
                center.x + randomCircle.x,
                center.y,
                center.z + randomCircle.y
            );

            NavMeshHit hit;

            if (NavMesh.SamplePosition(randomPosition, out hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        StartWaiting();
    }
}