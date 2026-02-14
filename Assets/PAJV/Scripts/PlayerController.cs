using UnityEngine;
using DarkRift;
using DarkRift.Client.Unity;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    private UnityClient client;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotSpeed = 150f;
    [SerializeField] private float airControl = 0.4f; // 0..1

    [Header("Jump")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float groundProbeDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0; // set Ground layer if you have one

    [Header("Networking")]
    [SerializeField] private float posSendThreshold = 0.05f;
    [SerializeField] private float rotSendThreshold = 1f;

    public bool InputDisabled = false;

    private Rigidbody rb;
    private Collider col;

    private Vector2 moveInput;     // x = strafe, y = forward
    private float rotateInput;     // -1..1
    private bool jumpQueued;

    private Vector3 lastSentPos;
    private float lastSentYaw;

    public void Initialize(UnityClient c)
    {
        client = c;

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        lastSentPos = transform.position;
        lastSentYaw = transform.eulerAngles.y;

        enabled = true;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        enabled = false; // don’t run until Initialize sets client (prevents nulls)
    }

    private void Update()
    {
        if (InputDisabled) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // WASD strafing + forward/back
        float x = 0f;
        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;

        float y = 0f;
        if (kb.wKey.isPressed) y += 1f;
        if (kb.sKey.isPressed) y -= 1f;

        moveInput = new Vector2(x, y);
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        // Rotate with Q/E (keep if you want)
        rotateInput = 0f;
        if (kb.qKey.isPressed) rotateInput -= 1f;
        if (kb.eKey.isPressed) rotateInput += 1f;

        // Queue jump for FixedUpdate
        if (kb.spaceKey.wasPressedThisFrame)
            jumpQueued = true;

        TrySendMovement();
    }

    private void FixedUpdate()
    {
        if (InputDisabled) return;

        bool grounded = IsGrounded(out RaycastHit hit);

        // Build desired planar velocity in world space (strafe + forward)
        Vector3 desired =
            (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;

        // Current velocity
        Vector3 v = rb.linearVelocity;

        // Apply move (full control on ground, partial in air)
        float control = grounded ? 1f : airControl;
        Vector3 newPlanar = Vector3.Lerp(new Vector3(v.x, 0f, v.z), new Vector3(desired.x, 0f, desired.z), control);

        rb.linearVelocity = new Vector3(newPlanar.x, v.y, newPlanar.z);

        // Rotate
        if (Mathf.Abs(rotateInput) > 0.001f)
        {
            float yaw = rotateInput * rotSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));
        }

        // Jump
        if (jumpQueued)
        {
            jumpQueued = false;

            if (grounded)
            {
                // Make jump consistent even if sliding down small slopes
                if (rb.linearVelocity.y < 0f)
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
    }

    private bool IsGrounded(out RaycastHit hit)
    {
        // SphereCast from just above the bottom of your collider
        Bounds b = col.bounds;

        // Sphere radius: a bit smaller than collider extents so it doesn't snag edges
        float radius = Mathf.Max(0.05f, Mathf.Min(b.extents.x, b.extents.z) * 0.9f);

        // Start point slightly above bottom
        Vector3 origin = new Vector3(b.center.x, b.min.y + radius + 0.02f, b.center.z);

        float castDist = groundProbeDistance;

        bool grounded = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out hit,
            castDist,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        return grounded;
    }

    private void TrySendMovement()
    {
        if (client == null) return;

        float yaw = transform.eulerAngles.y;

        bool moved = Vector3.Distance(transform.position, lastSentPos) > posSendThreshold;
        bool rotated = Mathf.Abs(Mathf.DeltaAngle(yaw, lastSentYaw)) > rotSendThreshold;

        if (!moved && !rotated) return;

        lastSentPos = transform.position;
        lastSentYaw = yaw;

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(transform.position.x);
            writer.Write(transform.position.y);
            writer.Write(transform.position.z);
            writer.Write(yaw);

            using (Message msg = Message.Create(1, writer))
                client.SendMessage(msg, SendMode.Unreliable);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<Collider>();
        if (col == null) return;

        Bounds b = col.bounds;
        float radius = Mathf.Max(0.05f, Mathf.Min(b.extents.x, b.extents.z) * 0.9f);
        Vector3 origin = new Vector3(b.center.x, b.min.y + radius + 0.02f, b.center.z);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, radius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundProbeDistance);
    }
#endif
}
