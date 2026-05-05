using UnityEngine;

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

    [System.Serializable]
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

    [Header("References")]
    [SerializeField] private CharacterController controller;

    [Header("Movement Detection")]
    [SerializeField] private float minMoveSpeed = 0.15f;
    [SerializeField] private float sprintSpeedThreshold = 9f;
    [SerializeField] private float crouchHeightThreshold = 2.1f;
    [SerializeField] private float crawlHeightThreshold = 1.3f;

    [Header("Base Sound Radius")]
    [SerializeField] private float walkRadius = 6f;
    [SerializeField] private float sprintRadius = 16f;
    [SerializeField] private float crouchRadius = 3.5f;
    [SerializeField] private float crawlRadius = 2f;
    [SerializeField] private float airborneRadius = 1f;

    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private SurfaceSoundProfile[] surfaceProfiles;

    [Header("Debug")]
    [SerializeField] private bool drawDebugCircle = true;
    [SerializeField] private bool onlyDrawWhenSprinting = true;
    [SerializeField] private int circleSegments = 96;
    [SerializeField] private float circleHeightOffset = 0.05f;

    public SoundField CurrentSoundField { get; private set; }

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
    }

    private SoundField CalculateSoundField()
    {
        MovementSoundType movementType = GetMovementSoundType();
        SurfaceSoundProfile surface = GetCurrentSurface();

        float baseRadius = GetBaseRadius(movementType);
        float surfaceMultiplier = surface != null ? surface.soundMultiplier : 1f;

        float finalRadius = baseRadius * surfaceMultiplier;

        return new SoundField
        {
            origin = transform.position,
            radius = finalRadius,
            loudness = Mathf.InverseLerp(0f, sprintRadius, finalRadius),
            movementType = movementType,
            surfaceName = surface != null ? surface.surfaceName : "Default"
        };
    }

    private MovementSoundType GetMovementSoundType()
    {
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

    private float GetBaseRadius(MovementSoundType movementType)
    {
        switch (movementType)
        {
            case MovementSoundType.Walking:
                return walkRadius;

            case MovementSoundType.Sprinting:
                return sprintRadius;

            case MovementSoundType.Crouching:
                return crouchRadius;

            case MovementSoundType.Crawling:
                return crawlRadius;

            case MovementSoundType.Airborne:
                return airborneRadius;

            default:
                return 0f;
        }
    }

    private SurfaceSoundProfile GetCurrentSurface()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
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

    public bool CanBeHeardFrom(Vector3 listenerPosition)
    {
        float distance = Vector3.Distance(CurrentSoundField.origin, listenerPosition);
        return distance <= CurrentSoundField.radius;
    }

    public float GetAudibilityAt(Vector3 listenerPosition)
    {
        float distance = Vector3.Distance(CurrentSoundField.origin, listenerPosition);

        if (CurrentSoundField.radius <= 0f)
            return 0f;

        return 1f - Mathf.Clamp01(distance / CurrentSoundField.radius);
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugCircle)
            return;

        SoundField field = Application.isPlaying ? CurrentSoundField : CalculateSoundField();

        if (field.radius <= 0f)
            return;

        if (onlyDrawWhenSprinting && field.movementType != MovementSoundType.Sprinting)
            return;

        Gizmos.DrawWireSphere(
            field.origin + Vector3.up * circleHeightOffset,
            field.radius
        );
    }
}