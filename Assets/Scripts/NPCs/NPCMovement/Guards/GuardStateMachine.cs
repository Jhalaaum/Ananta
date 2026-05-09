using UnityEngine;
using UnityEngine.AI;

public class GuardStateMachine : MonoBehaviour
{
    public enum GuardState
    {
        Patrol,
        InvestigateSound,
        Chase,
        SearchLastKnownPosition
    }

    [Header("Current State")]
    public GuardState currentState = GuardState.Patrol;

    [Header("Movement Speeds")]
    public float patrolSpeed = 2.5f;
    public float investigateSpeed = 3.2f;
    public float chaseSpeed = 5.2f;

    [Header("Detection")]
    public float catchDistance = 1.4f;
    public float lostPlayerAfterSeconds = 2.5f;
    public float searchTime = 4f;
    public float destinationReachDistance = 0.8f;

    [Header("Hearing")]
    public float minAudibilityToInvestigate = 0.12f;
    public float sprintAudibilityMultiplier = 1.6f;
    public float crouchAudibilityMultiplier = 0.5f;
    public float crawlAudibilityMultiplier = 0.25f;

    [Header("Visual Debug")]
    public Renderer guardRenderer;
    public GameObject alertIcon;

    public Color patrolColor = Color.white;
    public Color investigateColor = Color.yellow;
    public Color chaseColor = Color.red;
    public Color searchColor = new Color(1f, 0.5f, 0f);

    private NavMeshAgent agent;
    private GuardVision vision;
    private PatrolMovement patrolMovement;
    private Transform player;

    private Vector3 soundPosition;
    private Vector3 lastKnownPlayerPosition;

    private float lostPlayerTimer;
    private float searchTimer;

    private GuardState previousState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<GuardVision>();
        patrolMovement = GetComponent<PatrolMovement>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        if (guardRenderer == null)
        {
            guardRenderer = GetComponentInChildren<Renderer>();
        }
    }

    private void OnEnable()
    {
        PlayerSoundEmitter.OnSoundEmitted += HandlePlayerSound;
    }

    private void OnDisable()
    {
        PlayerSoundEmitter.OnSoundEmitted -= HandlePlayerSound;
    }

    private void Start()
    {
        EnterPatrol();
    }

    private void Update()
    {
        if (vision != null && vision.CanSeePlayer)
        {
            lastKnownPlayerPosition = vision.LastKnownPlayerPosition;
            EnterChase();
        }

        switch (currentState)
        {
            case GuardState.Patrol:
                UpdatePatrol();
                break;

            case GuardState.InvestigateSound:
                UpdateInvestigateSound();
                break;

            case GuardState.Chase:
                UpdateChase();
                break;

            case GuardState.SearchLastKnownPosition:
                UpdateSearchLastKnownPosition();
                break;
        }

        if (previousState != currentState)
        {
            Debug.Log(gameObject.name + " state changed to: " + currentState);
            previousState = currentState;
        }
    }

    private void HandlePlayerSound(PlayerSoundEmitter.SoundField sound)
    {
        if (currentState == GuardState.Chase)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, sound.origin);

        if (distance > sound.radius)
        {
            return;
        }

        float distanceAudibility = 1f - Mathf.Clamp01(distance / sound.radius);
        float finalAudibility = distanceAudibility * sound.loudness;

        finalAudibility *= GetMovementSoundMultiplier(sound.movementType);

        if (finalAudibility < minAudibilityToInvestigate)
        {
            return;
        }

        soundPosition = sound.origin;

        Debug.Log(gameObject.name + " heard " + sound.movementType + " on " + sound.surfaceName);

        EnterInvestigateSound();
    }

    private float GetMovementSoundMultiplier(PlayerSoundEmitter.MovementSoundType movementType)
    {
        switch (movementType)
        {
            case PlayerSoundEmitter.MovementSoundType.Sprinting:
                return sprintAudibilityMultiplier;

            case PlayerSoundEmitter.MovementSoundType.Crouching:
                return crouchAudibilityMultiplier;

            case PlayerSoundEmitter.MovementSoundType.Crawling:
                return crawlAudibilityMultiplier;

            default:
                return 1f;
        }
    }

    private void EnterPatrol()
    {
        currentState = GuardState.Patrol;

        agent.speed = patrolSpeed;

        if (patrolMovement != null)
        {
            patrolMovement.enabled = true;
        }

        SetVisualState(patrolColor, false);
    }

    private void UpdatePatrol()
    {
        // PatrolMovement script handles patrol movement.
    }

    private void EnterInvestigateSound()
    {
        currentState = GuardState.InvestigateSound;

        agent.speed = investigateSpeed;

        if (patrolMovement != null)
        {
            patrolMovement.enabled = false;
        }

        agent.SetDestination(soundPosition);

        SetVisualState(investigateColor, true);
    }

    private void UpdateInvestigateSound()
    {
        if (ReachedDestination())
        {
            lastKnownPlayerPosition = soundPosition;
            EnterSearchLastKnownPosition();
        }
    }

    private void EnterChase()
    {
        currentState = GuardState.Chase;

        agent.speed = chaseSpeed;
        lostPlayerTimer = lostPlayerAfterSeconds;

        if (patrolMovement != null)
        {
            patrolMovement.enabled = false;
        }

        SetVisualState(chaseColor, true);
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            EnterSearchLastKnownPosition();
            return;
        }

        if (vision != null && vision.CanSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            agent.SetDestination(player.position);
            lostPlayerTimer = lostPlayerAfterSeconds;
        }
        else
        {
            agent.SetDestination(lastKnownPlayerPosition);
            lostPlayerTimer -= Time.deltaTime;

            if (lostPlayerTimer <= 0f)
            {
                EnterSearchLastKnownPosition();
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= catchDistance)
        {
            Debug.Log("PLAYER CAUGHT by " + gameObject.name);
        }
    }

    private void EnterSearchLastKnownPosition()
    {
        currentState = GuardState.SearchLastKnownPosition;

        agent.speed = investigateSpeed;
        searchTimer = searchTime;

        if (patrolMovement != null)
        {
            patrolMovement.enabled = false;
        }

        agent.SetDestination(lastKnownPlayerPosition);

        SetVisualState(searchColor, true);
    }

    private void UpdateSearchLastKnownPosition()
    {
        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            EnterPatrol();
        }
    }

    private bool ReachedDestination()
    {
        if (agent.pathPending)
        {
            return false;
        }

        return agent.remainingDistance <= destinationReachDistance;
    }

    private void SetVisualState(Color color, bool showAlertIcon)
    {
        if (guardRenderer != null)
        {
            guardRenderer.material.color = color;
        }

        if (alertIcon != null)
        {
            alertIcon.SetActive(showAlertIcon);
        }
    }
}