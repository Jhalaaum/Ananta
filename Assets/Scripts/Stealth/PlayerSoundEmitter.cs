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
        Airborne
    }

    [Serializable]
    public class MovementSoundProfile
    {
        public MovementSoundType movementType;
        public float radius = 5f;
        public float emitInterval = 0.4f;
        public float loudness = 0.5f;
    }

    [Serializable]
    public class SurfaceSoundProfile
    {
        public string surfaceName = "Stone";
        public PhysicsMaterial physicMaterial;
        public LayerMask layerMask;
        public float soundMultiplier = 1f;
    }

    public struct SoundField
    {
        public Vector3 origin;
        public float radius;
        public float loudness;
        public MovementSoundType movementType;
        public string surfaceName;
    }

    public static event Action<SoundField> OnSoundEmitted;

    [Header("References")]
    [SerializeField] private CharacterController controller;

    [Header("Movement Detection")]
    [SerializeField] private float minMoveSpeed = 0.15f;
    [SerializeField] private float sprintSpeedThreshold = 9f;
    [SerializeField] private float crouchHeightThreshold = 2.1f;
    [SerializeField] private float crawlHeightThreshold = 1.3f;

    [Header("Movement Sound Profiles")]
    [SerializeField] private MovementSoundProfile[] movementProfiles =
    {
        new MovementSoundProfile { movementType = MovementSoundType.Walking, radius = 6f, emitInterval = 0.45f, loudness = 0.45f },
        new MovementSoundProfile { movementType = MovementSoundType.Sprinting, radius = 16f, emitInterval = 0.25f, loudness = 1f },
        new MovementSoundProfile { movementType = MovementSoundType.Crouching, radius = 3.5f, emitInterval = 0.7f, loudness = 0.25f },
        new MovementSoundProfile { movementType = MovementSoundType.Crawling, radius = 2f, emitInterval = 0.9f, loudness = 0.15f },
        new MovementSoundProfile { movementType = MovementSoundType.Airborne, radius = 1f, emitInterval = 1f, loudness = 0.1f }
    };

    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private SurfaceSoundProfile[] surfaceProfiles;

    [Header("Occlusion")]
    [SerializeField] private bool useOcclusion = true;
    [SerializeField] private LayerMask occlusionMask;
    [SerializeField] private float occludedLoudnessMultiplier = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugCircle = true;
    [SerializeField] private bool onlyDrawWhenSprinting = false;
    [SerializeField] private Color debugColor = Color.yellow;
    [SerializeField] private float circleHeightOffset = 0.05f;

    public SoundField CurrentSoundField { get; private set; }

    private float nextEmitTime;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        CurrentSoundField = CalculateSoundField();

        if (CurrentSoundField.movementType == MovementSoundType.Silent)
            return;

        MovementSoundProfile profile = GetMovementProfile(CurrentSoundField.movementType);

        if (profile == null)
            return;

        if (Time.time >= nextEmitTime)
        {
            EmitSound(CurrentSoundField);
            nextEmitTime = Time.time + profile.emitInterval;
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
                radius = 0f,
                loudness = 0f,
                movementType = MovementSoundType.Silent,
                surfaceName = surfaceProfile != null ? surfaceProfile.surfaceName : "Default"
            };
        }

        float surfaceMultiplier = surfaceProfile != null ? surfaceProfile.soundMultiplier : 1f;

        return new SoundField
        {
            origin = transform.position,
            radius = movementProfile.radius * surfaceMultiplier,
            loudness = Mathf.Clamp01(movementProfile.loudness * surfaceMultiplier),
            movementType = movementType,
            surfaceName = surfaceProfile != null ? surfaceProfile.surfaceName : "Default"
        };
    }

    private MovementSoundType GetMovementSoundType()
    {
        if (controller == null)
            return MovementSoundType.Silent;

        if (!controller.isGrounded)
            return MovementSoundType.Airborne;

        Vector3 flatVelocity = controller.velocity;
        flatVelocity.y = 0f;

        float speed = flatVelocity.magnitude;

        if (speed < minMoveSpeed)
            return MovementSoundType.Silent;

        if (controller.height <= crawlHeightThreshold)
            return MovementSoundType.Crawling;

        if (controller.height <= crouchHeightThreshold)
            return MovementSoundType.Crouching;

        if (speed >= sprintSpeedThreshold)
            return MovementSoundType.Sprinting;

        return MovementSoundType.Walking;
    }

    private MovementSoundProfile GetMovementProfile(MovementSoundType movementType)
    {
        if (movementProfiles == null)
            return null;

        foreach (MovementSoundProfile profile in movementProfiles)
        {
            if (profile != null && profile.movementType == movementType)
                return profile;
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
            return null;

        foreach (SurfaceSoundProfile profile in surfaceProfiles)
        {
            if (profile == null)
                continue;

            bool materialMatches =
                profile.physicMaterial != null &&
                hit.collider.sharedMaterial == profile.physicMaterial;

            bool layerMatches =
                profile.layerMask.value != 0 &&
                ((1 << hit.collider.gameObject.layer) & profile.layerMask.value) != 0;

            if (materialMatches || layerMatches)
                return profile;
        }

        return null;
    }

    private void EmitSound(SoundField field)
    {
        OnSoundEmitted?.Invoke(field);
    }

    public bool CanBeHeardFrom(Vector3 listenerPosition)
    {
        return GetAudibilityAt(listenerPosition) > 0f;
    }

    public float GetAudibilityAt(Vector3 listenerPosition)
    {
        SoundField field = CurrentSoundField;

        if (field.radius <= 0f)
            return 0f;

        float distance = Vector3.Distance(field.origin, listenerPosition);

        if (distance > field.radius)
            return 0f;

        float distanceAudibility = 1f - Mathf.Clamp01(distance / field.radius);
        float finalAudibility = distanceAudibility * field.loudness;

        if (useOcclusion && IsOccluded(listenerPosition))
            finalAudibility *= occludedLoudnessMultiplier;

        return Mathf.Clamp01(finalAudibility);
    }

    private bool IsOccluded(Vector3 listenerPosition)
    {
        Vector3 start = transform.position + Vector3.up * 1.2f;
        Vector3 end = listenerPosition + Vector3.up * 1.2f;

        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return false;

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
            return;

        if (controller == null)
            controller = GetComponent<CharacterController>();

        SoundField field = Application.isPlaying ? CurrentSoundField : CalculateSoundField();

        if (field.radius <= 0f)
            return;

        if (onlyDrawWhenSprinting && field.movementType != MovementSoundType.Sprinting)
            return;

        Gizmos.color = debugColor;
        Gizmos.DrawWireSphere(
            field.origin + Vector3.up * circleHeightOffset,
            field.radius
        );
    }

    private void OnValidate()
    {
        minMoveSpeed = Mathf.Max(0f, minMoveSpeed);
        sprintSpeedThreshold = Mathf.Max(0f, sprintSpeedThreshold);
        groundCheckDistance = Mathf.Max(0.1f, groundCheckDistance);
        occludedLoudnessMultiplier = Mathf.Clamp01(occludedLoudnessMultiplier);

        if (controller == null)
            controller = GetComponent<CharacterController>();
    }
}