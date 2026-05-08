using UnityEngine;
using UnityEngine.AI;

public class GuardChase : MonoBehaviour
{
    [Header("Chase Settings")]
    public float chaseSpeed = 4f;
    public float patrolSpeed = 2.5f;
    public float lostPlayerTime = 3f;
    public float catchDistance = 1.3f;

    private NavMeshAgent agent;
    private GuardVision guardVision;
    private PatrolMovement patrolMovement;
    private Transform player;

    private bool isChasing;
    private float lostTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        guardVision = GetComponent<GuardVision>();
        patrolMovement = GetComponent<PatrolMovement>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        agent.speed = patrolSpeed;
    }

    void Update()
    {
        if (player == null || guardVision == null)
        {
            return;
        }

        if (guardVision.CanSeePlayer)
        {
            StartChase();
        }

        if (isChasing)
        {
            ChasePlayer();
        }
    }

    void StartChase()
    {
        isChasing = true;
        lostTimer = lostPlayerTime;

        if (patrolMovement != null)
        {
            patrolMovement.enabled = false;
        }

        agent.speed = chaseSpeed;
    }

    void ChasePlayer()
    {
        if (guardVision.CanSeePlayer)
        {
            agent.SetDestination(player.position);
            lostTimer = lostPlayerTime;
        }
        else
        {
            agent.SetDestination(guardVision.LastKnownPlayerPosition);
            lostTimer -= Time.deltaTime;

            if (lostTimer <= 0f)
            {
                StopChaseAndReturnToPatrol();
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= catchDistance)
        {
            Debug.Log("Player caught by " + gameObject.name);
        }
    }

    void StopChaseAndReturnToPatrol()
    {
        isChasing = false;
        agent.speed = patrolSpeed;

        if (patrolMovement != null)
        {
            patrolMovement.enabled = true;
        }
    }
}