using UnityEngine;

// ============================================================
//  PlayerController.cs
//  Movimiento top-down con perspectiva elevada (estilo Eastward).
//  
//  SETUP:
//  1. Crea un GameObject "Player"
//  2. Añade este script + Rigidbody + CapsuleCollider
//  3. Rigidbody: Freeze Rotation X, Y, Z  |  Use Gravity = true
//  4. En Player, pon un hijo llamado "Sprite" con SpriteRenderer
//     (el sprite se volteará automáticamente según dirección)
// ============================================================

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    // ── Stats de movimiento ───────────────────────────────
    [Header("=== Movimiento ===")]
    [Tooltip("Velocidad base de caminar")]
    public float walkSpeed = 4f;

    [Tooltip("Multiplicador al correr (Shift)")]
    public float runMultiplier = 1.8f;

    [Tooltip("Qué tan rápido el personaje alcanza la velocidad objetivo (0=rígido, 1=suave)")]
    [Range(0f, 1f)]
    public float movementSmoothing = 0.12f;

    [Tooltip("Cuánto tarda en detenerse al soltar las teclas")]
    [Range(0f, 1f)]
    public float decelerationSmoothing = 0.08f;

    // ── Referencias ───────────────────────────────────────
    [Header("=== Referencias ===")]
    [Tooltip("Transform del sprite hijo (para flip horizontal)")]
    public Transform spriteTransform;

    [Tooltip("Animator del sprite (puede ser null si no hay animaciones aún)")]
    public Animator animator;

    // ── Suelo y terreno ───────────────────────────────────
    [Header("=== Suelo ===")]
    [Tooltip("Distancia al suelo para considerar que está grounded")]
    public float groundCheckDistance = 0.15f;
    public LayerMask groundLayer;

    // ── Privados ──────────────────────────────────────────
    private Rigidbody rb;
    private Vector3 targetVelocity;
    private Vector3 smoothedVelocity;
    private Vector3 velocityRef;           // referencia para SmoothDamp
    private bool isGrounded;
    private bool isRunning;
    private float currentSpeedMultiplier = 1f; // modificado por tiles (agua, arena…)
    private Vector2 lastMoveDir = Vector2.right;

    // Hash de parámetros del Animator (más eficiente que strings)
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int HashDirX = Animator.StringToHash("DirX");
    private static readonly int HashDirY = Animator.StringToHash("DirY");

    // ── Propiedades públicas ──────────────────────────────
    public Vector2 MoveDirection => lastMoveDir;
    public bool IsMoving => smoothedVelocity.magnitude > 0.1f;
    public bool IsRunning => isRunning;

    // ─────────────────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // movimiento suave
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (spriteTransform == null)
            spriteTransform = transform.Find("Sprite");

        if (animator == null && spriteTransform != null)
            animator = spriteTransform.GetComponent<Animator>();
    }

    void Update()
    {
        ReadInput();
        HandleSpriteFlip();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        CheckGrounded();
        ApplyMovement();
    }

    // ── Input ─────────────────────────────────────────────
    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        Vector3 rawDir = new Vector3(h, 0f, v).normalized;

        float speed = walkSpeed * (isRunning ? runMultiplier : 1f) * currentSpeedMultiplier;
        targetVelocity = rawDir * speed;

        if (rawDir.magnitude > 0.1f)
            lastMoveDir = new Vector2(h, v).normalized;
    }

    // ── Movimiento físico ─────────────────────────────────
    void ApplyMovement()
    {
        float smooth = targetVelocity.magnitude > 0.01f ? movementSmoothing : decelerationSmoothing;

        smoothedVelocity = Vector3.SmoothDamp(
            smoothedVelocity,
            targetVelocity,
            ref velocityRef,
            smooth
        );

        // Conservamos la velocidad Y (gravedad)
        Vector3 newVel = smoothedVelocity;
        newVel.y = rb.linearVelocity.y;
        rb.linearVelocity = newVel;
    }

    // ── Grounded check ────────────────────────────────────
    void CheckGrounded()
    {
        isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer
        );
    }

    // ── Sprite flip horizontal ────────────────────────────
    void HandleSpriteFlip()
    {
        if (spriteTransform == null) return;
        if (Mathf.Abs(lastMoveDir.x) < 0.01f) return;

        // Flip: si va a la izquierda invertimos escala X
        Vector3 scale = spriteTransform.localScale;
        scale.x = lastMoveDir.x < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        spriteTransform.localScale = scale;
    }

    // ── Animator ──────────────────────────────────────────
    void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = smoothedVelocity.magnitude;
        animator.SetFloat(HashSpeed, speed);
        animator.SetBool(HashIsMoving, speed > 0.1f);
        animator.SetFloat(HashDirX, lastMoveDir.x);
        animator.SetFloat(HashDirY, lastMoveDir.y);
    }

    // ── API pública ───────────────────────────────────────

    /// <summary>
    /// Llamado por TileManager cuando el jugador pisa un tile especial.
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        currentSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// Teletransporta al jugador a una posición (ej: cambio de zona).
    /// </summary>
    public void Teleport(Vector3 position)
    {
        rb.linearVelocity = Vector3.zero;
        smoothedVelocity = Vector3.zero;
        transform.position = position;
    }
}