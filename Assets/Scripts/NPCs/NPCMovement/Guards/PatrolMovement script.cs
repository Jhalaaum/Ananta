using UnityEngine;
using UnityEngine.AI;

public class PatrolMovement : MonoBehaviour
{
    public Transform patrolRouteParent;

    private NavMeshAgent agent;
    private Transform[] patrolPoints;
    private int currentPointIndex = 0;

    public float reachDistance = 0.5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        int childCount = patrolRouteParent.childCount;
        patrolPoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            patrolPoints[i] = patrolRouteParent.GetChild(i);
        }

        GoToCurrentPoint();
    }

    void Update()
    {
        if (patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= reachDistance)
        {
            GoToNextPoint();
        }
    }

    void GoToCurrentPoint()
    {
        agent.SetDestination(patrolPoints[currentPointIndex].position);
    }

    void GoToNextPoint()
    {
        currentPointIndex++;

        if (currentPointIndex >= patrolPoints.Length)
        {
            currentPointIndex = 0;
        }

        GoToCurrentPoint();
    }
}