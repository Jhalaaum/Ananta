using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerSoundEmitter : MonoBehaviour
{
    public enum MovementSoundType
    {
        Silent,
        Walking,
        Sprinting,
        Crouching,
        Crawling,
        Airborne,
        Landing
    }

    public enum MovementDetectionMode
    {
        Auto,
        PlayerMovementStateTracker,
        CharacterControllerHeight
    }

    [Serializable]
    public class MovementSoundProfile
    {
        public MovementSoundType movementType;
        public float radius = 5f;
        public float emitInterval = 0.4f;
        [Range(0f, 1f)] public float loudness = 0.5f;

        [Header("Investigation Accuracy")]
        public float soundOriginUncertainty = 1f;
    }

    [Serializable]
    public class SurfaceSoundProfile
    {
        public string surfaceName = "Stone";
        public PhysicsMaterial physicMaterial;
        public LayerMask layerMask;
        public float soundMultiplier = 1f;
        public float uncertaintyMultiplier = 1f;
    }

    public struct SoundField
    {
        public Vector3 origin;
        public Vector3 trueOrigin;

        public float radius;
        public float loudness;

        public MovementSoundType movementType;
        public string surfaceName;

        public float confidence;
        public float uncertainty;
    }

    public static event Action<SoundField> OnSoundEmitted;

    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerMovementStateTracker movementStateTracker;

    [Header("Movement Detection")]
    [SerializeField] private MovementDetectionMode detectionMode = MovementDetectionMode.Auto;
    [SerializeField] private float minMoveSpeed = 0.15f;
    [SerializeField] private float sprintSpeedThreshold = 9f;
    [SerializeField] private float crouchHeightThreshold = 2.1f;
    [SerializeField] private float crawlHeightThreshold = 1.3f;

    [Header("Movement Sound Profiles")]
    [SerializeField] private MovementSoundProfile[] movementProfiles =
    {
        new MovementSoundProfile
        {
            movementType = MovementSoundType.Walking,
            radius = 6f,
            emitInterval = 0.45f,
            loudness = 0.45f,
            soundOriginUncertainty = 1.6f
        },

        new MovementSoundProfile
        {
            movementType = MovementSoundType.Sprinting,
            radius = 16f,
            emitInterval = 0.25f,
            loudness = 1f,
            soundOriginUncertainty = 0.6f
        },

        new MovementSoundProfile
        {
            movementType = MovementSoundType.Crouching,
            radius = 3.5f,
            emitInterval = 0.7f,
            loudness = 0.25f,
            soundOriginUncertainty = 2.6f
        },

        new MovementSoundProfile
        {
            movementType = MovementSoundType.Crawling,
            radius = 2f,
            emitInterval = 0.9f,
            loudness = 0.15f,
            soundOriginUncertainty = 3.2f
        },

        new MovementSoundProfile
        {
            movementType = MovementSoundType.Airborne,
            radius = 1f,
            emitInterval = 1f,
            loudness = 0.1f,
            soundOriginUncertainty = 2f
        },

        new MovementSoundProfile
        {
            movementType = MovementSoundType.Landing,
            radius = 10f,
            emitInterval = 0.1f,
            loudness = 0.75f,
            soundOriginUncertainty = 0.8f
        }
    };

    [Header("Landing Sound")]
    [SerializeField] private bool emitLandingSound = true;
    [SerializeField] private float minimumFallSpeedForLandingSound = -3f;
    [SerializeField] private float landingSoundCooldown = 0.25f;

    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private SurfaceSoundProfile[] surfaceProfiles;

    [Header("Sound Origin Realism")]
    [SerializeField] private bool useApproximateSoundOrigin = true;
    [SerializeField] private float globalUncertaintyMultiplier = 1f;
    [SerializeField] private float minimumUncertainty = 0.15f;
    [SerializeField] private float maximumUncertainty = 5f;

    [Header("Occlusion")]
    [SerializeField] private bool useOcclusion = true;
    [SerializeField] private LayerMask occlusionMask;
    [SerializeField] private float occludedLoudnessMultiplier = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool drawDebugCircle = true;
    [SerializeField] private bool onlyDrawWhenSprinting = false;
    [SerializeField] private Color debugColor = Color.yellow;
    [SerializeField] private Color trueOriginDebugColor = Color.cyan;
    [SerializeField] private float circleHeightOffset = 0.05f;

    public SoundField CurrentSoundField { get; private set; }
    public MovementSoundType CurrentMovementSoundType { get; private set; }

    private float nextEmitTime;
    private float lastLandingSoundTime = -999f;

    private bool wasGrounded;
    private float previousVerticalVelocity;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
        movementStateTracker = GetComponent<PlayerMovementStateTracker>();
    }

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        if (movementStateTracker == null)
        {
            movementStateTracker = GetComponent<PlayerMovementStateTracker>();
        }

        wasGrounded = controller != null && controller.isGrounded;
    }

    private void Update()
    {
        CurrentSoundField = CalculateSoundField();
        CurrentMovementSoundType = CurrentSoundField.movementType;

        TryEmitLandingSound();

        if (CurrentSoundField.movementType == MovementSoundType.Silent)
        {
            return;
        }

        if (CurrentSoundField.movementType == MovementSoundType.Landing)
        {
            return;
        }

        MovementSoundProfile profile = GetMovementProfile(CurrentSoundField.movementType);

        if (profile == null)
        {
            return;
        }

        if (Time.time >= nextEmitTime)
        {
            EmitSound(CurrentSoundField);
            nextEmitTime = Time.time + profile.emitInterval;
        }

        if (controller != null)
        {
            previousVerticalVelocity = controller.velocity.y;
            wasGrounded = controller.isGrounded;
        }
    }

    private SoundField CalculateSoundField()
    {
        MovementSoundType movementType = GetMovementSoundType();
        MovementSoundProfile movementProfile = GetMovementProfile(movementType);
        SurfaceSoundProfile surfaceProfile = GetCurrentSurface();

        if (movementProfile == null || movementType == MovementSoundType.Silent)
        {
            return new SoundField
            {
                origin = transform.position,
                trueOrigin = transform.position,
                radius = 0f,
                loudness = 0f,
                movementType = MovementSoundType.Silent,
                surfaceName = surfaceProfile != null ? surfaceProfile.surfaceName : "Default",
                confidence = 0f,
                uncertainty = 0f
            };
        }

        float surfaceMultiplier = surfaceProfile != null
            ? Mathf.Max(0f, surfaceProfile.soundMultiplier)
            : 1f;

        float uncertaintyMultiplier = surfaceProfile != null
            ? Mathf.Max(0f, surfaceProfile.uncertaintyMultiplier)
            : 1f;

        float finalRadius = movementProfile.radius * surfaceMultiplier;
        float finalLoudness = Mathf.Clamp01(movementProfile.loudness * surfaceMultiplier);

        float confidence = Mathf.Clamp01(finalLoudness);

        float uncertainty =
            movementProfile.soundOriginUncertainty *
            globalUncertaintyMultiplier *
            uncertaintyMultiplier;

        uncertainty = Mathf.Clamp(
            uncertainty,
            minimumUncertainty,
            maximumUncertainty
        );

        Vector3 trueOrigin = transform.position;
        Vector3 perceivedOrigin = trueOrigin;

        if (useApproximateSoundOrigin)
        {
            perceivedOrigin = GetApproximateSoundOrigin(trueOrigin, uncertainty, confidence);
        }

        return new SoundField
        {
            origin = perceivedOrigin,
            trueOrigin = trueOrigin,
            radius = finalRadius,
            loudness = finalLoudness,
            movementType = movementType,
            surfaceName = surfaceProfile != null ? surfaceProfile.surfaceName : "Default",
            confidence = confidence,
            uncertainty = uncertainty
        };
    }

    private MovementSoundType GetMovementSoundType()
    {
        if (controller == null)
        {
            return MovementSoundType.Silent;
        }

        if (!controller.isGrounded)
        {
            return MovementSoundType.Airborne;
        }

        Vector3 flatVelocity = controller.velocity;
        flatVelocity.y = 0f;

        float speed = flatVelocity.magnitude;

        if (speed < minMoveSpeed)
        {
            return MovementSoundType.Silent;
        }

        if (ShouldUseMovementStateTracker())
        {
            MovementSoundType trackerType = GetMovementTypeFromTracker(speed);

            if (trackerType != MovementSoundType.Silent)
            {
                return trackerType;
            }
        }

        return GetMovementTypeFromControllerHeight(speed);
    }

    private bool ShouldUseMovementStateTracker()
    {
        if (movementStateTracker == null)
        {
            return false;
        }

        return detectionMode == MovementDetectionMode.Auto ||
               detectionMode == MovementDetectionMode.PlayerMovementStateTracker;
    }

    private MovementSoundType GetMovementTypeFromTracker(float speed)
    {
        if (movementStateTracker == null)
        {
            return MovementSoundType.Silent;
        }

        PlayerMovementVisualState visualState = movementStateTracker.GetCurrentState();

        switch (visualState)
        {
            case PlayerMovementVisualState.Idle:
                return MovementSoundType.Silent;

            case PlayerMovementVisualState.Walking:
                return MovementSoundType.Walking;

            case PlayerMovementVisualState.Running:
                return MovementSoundType.Sprinting;

            case PlayerMovementVisualState.Crouching:
                return MovementSoundType.Crouching;

            case PlayerMovementVisualState.Crawling:
                return MovementSoundType.Crawling;

            case PlayerMovementVisualState.Jumping:
            case PlayerMovementVisualState.Falling:
                return MovementSoundType.Airborne;

            default:
                break;
        }

        if (speed >= sprintSpeedThreshold)
        {
            return MovementSoundType.Sprinting;
        }

        if (speed >= minMoveSpeed)
        {
            return MovementSoundType.Walking;
        }

        return MovementSoundType.Silent;
    }

    private MovementSoundType GetMovementTypeFromControllerHeight(float speed)
    {
        if (controller.height <= crawlHeightThreshold)
        {
            return MovementSoundType.Crawling;
        }

        if (controller.height <= crouchHeightThreshold)
        {
            return MovementSoundType.Crouching;
        }

        if (speed >= sprintSpeedThreshold)
        {
            return MovementSoundType.Sprinting;
        }

        return MovementSoundType.Walking;
    }

    private Vector3 GetApproximateSoundOrigin(
        Vector3 trueOrigin,
        float uncertainty,
        float confidence
    )
    {
        float confidenceAdjustedUncertainty = Mathf.Lerp(
            uncertainty,
            uncertainty * 0.35f,
            confidence
        );

        Vector2 offset = UnityEngine.Random.insideUnitCircle * confidenceAdjustedUncertainty;

        return new Vector3(
            trueOrigin.x + offset.x,
            trueOrigin.y,
            trueOrigin.z + offset.y
        );
    }

    private void TryEmitLandingSound()
    {
        if (!emitLandingSound)
        {
            return;
        }

        if (controller == null)
        {
            return;
        }

        bool isGroundedNow = controller.isGrounded;

        bool justLanded = !wasGrounded && isGroundedNow;
        bool fallWasHardEnough = previousVerticalVelocity <= minimumFallSpeedForLandingSound;
        bool cooldownReady = Time.time >= lastLandingSoundTime + landingSoundCooldown;

        if (!justLanded || !fallWasHardEnough || !cooldownReady)
        {
            previousVerticalVelocity = controller.velocity.y;
            wasGrounded = isGroundedNow;
            return;
        }

        MovementSoundProfile landingProfile = GetMovementProfile(MovementSoundType.Landing);

        if (landingProfile == null)
        {
            return;
        }

        SurfaceSoundProfile surfaceProfile = GetCurrentSurface();

        float surfaceMultiplier = surfaceProfile != null
            ? Mathf.Max(0f, surfaceProfile.soundMultiplier)
            : 1f;

        float finalRadius = landingProfile.radius * surfaceMultiplier;
        float finalLoudness = Mathf.Clamp01(landingProfile.loudness * surfaceMultiplier);

        float uncertainty = Mathf.Clamp(
            landingProfile.soundOriginUncertainty * globalUncertaintyMultiplier,
            minimumUncertainty,
            maximumUncertainty
        );

        Vector3 trueOrigin = transform.position;
        Vector3 perceivedOrigin = useApproximateSoundOrigin
            ? GetApproximateSoundOrigin(trueOrigin, uncertainty, finalLoudness)
            : trueOrigin;

        SoundField landingField = new SoundField
        {
            origin = perceivedOrigin,
            trueOrigin = trueOrigin,
            radius = finalRadius,
            loudness = finalLoudness,
            movementType = MovementSoundType.Landing,
            surfaceName = surfaceProfile != null ? surfaceProfile.surfaceName : "Default",
            confidence = finalLoudness,
            uncertainty = uncertainty
        };

        EmitSound(landingField);

        lastLandingSoundTime = Time.time;
        nextEmitTime = Time.time + landingSoundCooldown;

        if (showDebugLogs)
        {
            Debug.Log(
                $"[PlayerSoundEmitter] Landing sound. " +
                $"Surface: {landingField.surfaceName}, " +
                $"Radius: {landingField.radius:0.0}, " +
                $"Loudness: {landingField.loudness:0.00}"
            );
        }
    }

    private MovementSoundProfile GetMovementProfile(MovementSoundType movementType)
    {
        if (movementProfiles == null)
        {
            return null;
        }

        foreach (MovementSoundProfile profile in movementProfiles)
        {
            if (profile != null && profile.movementType == movementType)
            {
                return profile;
            }
        }

        return null;
    }

    private SurfaceSoundProfile GetCurrentSurface()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        if (!Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            return null;
        }

        if (surfaceProfiles == null)
        {
            return null;
        }

        foreach (SurfaceSoundProfile profile in surfaceProfiles)
        {
            if (profile == null)
            {
                continue;
            }

            bool materialMatches =
                profile.physicMaterial != null &&
                hit.collider.sharedMaterial == profile.physicMaterial;

            bool layerMatches =
                profile.layerMask.value != 0 &&
                ((1 << hit.collider.gameObject.layer) & profile.layerMask.value) != 0;

            if (materialMatches || layerMatches)
            {
                return profile;
            }
        }

        return null;
    }

    private void EmitSound(SoundField field)
    {
        OnSoundEmitted?.Invoke(field);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[PlayerSoundEmitter] Emitted {field.movementType} sound. " +
                $"Surface: {field.surfaceName}, " +
                $"Radius: {field.radius:0.0}, " +
                $"Loudness: {field.loudness:0.00}, " +
                $"Confidence: {field.confidence:0.00}, " +
                $"Uncertainty: {field.uncertainty:0.0}"
            );
        }
    }

    public bool CanBeHeardFrom(Vector3 listenerPosition)
    {
        return GetAudibilityAt(listenerPosition) > 0f;
    }

    public float GetAudibilityAt(Vector3 listenerPosition)
    {
        SoundField field = CurrentSoundField;

        if (field.radius <= 0f)
        {
            return 0f;
        }

        float distance = Vector3.Distance(field.trueOrigin, listenerPosition);

        if (distance > field.radius)
        {
            return 0f;
        }

        float distanceAudibility = 1f - Mathf.Clamp01(distance / field.radius);
        float finalAudibility = distanceAudibility * field.loudness;

        if (useOcclusion && IsOccluded(listenerPosition))
        {
            finalAudibility *= occludedLoudnessMultiplier;
        }

        return Mathf.Clamp01(finalAudibility);
    }

    private bool IsOccluded(Vector3 listenerPosition)
    {
        Vector3 start = transform.position + Vector3.up * 1.2f;
        Vector3 end = listenerPosition + Vector3.up * 1.2f;

        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            return false;
        }

        return Physics.Raycast(
            start,
            direction.normalized,
            distance,
            occlusionMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugCircle)
        {
            return;
        }

        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        SoundField field = Application.isPlaying
            ? CurrentSoundField
            : CalculateSoundField();

        if (field.radius <= 0f)
        {
            return;
        }

        if (onlyDrawWhenSprinting && field.movementType != MovementSoundType.Sprinting)
        {
            return;
        }

        Gizmos.color = debugColor;
        Gizmos.DrawWireSphere(
            field.origin + Vector3.up * circleHeightOffset,
            field.radius
        );

        Gizmos.color = trueOriginDebugColor;
        Gizmos.DrawWireSphere(
            field.trueOrigin + Vector3.up * circleHeightOffset,
            0.25f
        );

        Gizmos.DrawLine(
            field.trueOrigin + Vector3.up * circleHeightOffset,
            field.origin + Vector3.up * circleHeightOffset
        );
    }

    private void OnValidate()
    {
        minMoveSpeed = Mathf.Max(0f, minMoveSpeed);
        sprintSpeedThreshold = Mathf.Max(0f, sprintSpeedThreshold);
        groundCheckDistance = Mathf.Max(0.1f, groundCheckDistance);
        occludedLoudnessMultiplier = Mathf.Clamp01(occludedLoudnessMultiplier);
        globalUncertaintyMultiplier = Mathf.Max(0f, globalUncertaintyMultiplier);
        minimumUncertainty = Mathf.Max(0f, minimumUncertainty);
        maximumUncertainty = Mathf.Max(minimumUncertainty, maximumUncertainty);

        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        if (movementStateTracker == null)
        {
            movementStateTracker = GetComponent<PlayerMovementStateTracker>();
        }
    }
}