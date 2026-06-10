using UnityEngine;

// ============================================================
//  CameraController.cs
//  Cámara estilo Paper Mario / Eastward UE
//
//  La cámara está DETRÁS y ligeramente ARRIBA del jugador,
//  mirando levemente hacia abajo. El jugador se mueve en X y Z
//  (profundidad) y la cámara lo sigue suavemente.
//  Se ve casi como un juego 2D pero con profundidad real en Z.
//
//  VALORES RECOMENDADOS para el look de las referencias:
//    Distance      = 12
//    VerticalAngle = 15   (casi frontal, poquito elevada)
//    Height        = 3    (la cámara está 3 unidades arriba del jugador)
// ============================================================

public class CameraController : MonoBehaviour
{
    [Header("=== Objetivo ===")]
    public Transform target;

    [Header("=== Posición de la Cámara ===")]
    [Tooltip("Distancia hacia atrás (Z) desde el jugador")]
    public float distance = 12f;

    [Tooltip("Cuánto sube la cámara sobre el jugador en Y")]
    public float height = 3f;

    [Tooltip("Ángulo hacia abajo con que mira la cámara (0=horizontal, 15=Paper Mario)")]
    [Range(0f, 45f)]
    public float tiltAngle = 15f;

    [Header("=== Zoom con Scroll ===")]
    public bool enableScrollZoom = true;
    public float scrollZoomSpeed = 2f;
    public float minDistance = 5f;
    public float maxDistance = 22f;
    [Range(0.01f, 0.5f)]
    public float zoomSmoothing = 0.12f;

    [Header("=== Suavizado ===")]
    [Tooltip("Suavizado de seguimiento — 0.08 es bastante responsivo")]
    [Range(0.01f, 0.4f)]
    public float followSmoothing = 0.08f;

    [Tooltip("Suavizado extra en Y para que no salte en terreno irregular")]
    [Range(0.01f, 0.4f)]
    public float verticalSmoothing = 0.15f;

    [Header("=== Look-Ahead ===")]
    [Tooltip("Cuánto se adelanta la cámara en X cuando el jugador se mueve")]
    public float lookAheadX = 1.5f;
    [Tooltip("Cuánto se adelanta en Z (profundidad)")]
    public float lookAheadZ = 1.0f;
    [Range(0.01f, 0.5f)]
    public float lookAheadSmoothing = 0.2f;

    [Header("=== FOV ===")]
    public float defaultFOV = 60f;
    public float runFOV = 63f;
    [Range(0.01f, 0.3f)]
    public float fovSmoothing = 0.15f;

    [Header("=== Límites del Mapa ===")]
    public bool useBounds = false;
    public float boundsMinX = -30f;
    public float boundsMaxX = 30f;
    public float boundsMinZ = -30f;
    public float boundsMaxZ = 30f;

    // ── Privados ──────────────────────────────────────────
    private Camera cam;
    private PlayerController player;

    private float currentDistance;
    private float targetDistance;
    private float distVelocity;

    private float smoothY;
    private float yVelocity;

    private Vector3 smoothXZ;
    private Vector3 xzVelocity;

    private Vector3 lookAheadOffset;
    private Vector3 lookAheadVel;

    private float currentFOV;
    private float fovVelocity;

    private Vector3 shakeOffset;
    private float shakeTimer;
    private float shakeDuration;
    private float shakeMag;

    // ─────────────────────────────────────────────────────
    void Awake()
    {
        cam = GetComponent<Camera>();
        if (target != null)
            player = target.GetComponent<PlayerController>();

        currentDistance = distance;
        targetDistance = distance;
        currentFOV = defaultFOV;
        cam.fieldOfView = defaultFOV;

        if (target != null)
        {
            smoothXZ = new Vector3(target.position.x, 0f, target.position.z);
            smoothY = target.position.y;
            transform.position = GetDesiredPosition(target.position);
            transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleZoom();
        HandleLookAhead();
        HandleShake();
        HandleFOV();
        FollowTarget();
    }

    // ── Posición deseada de la cámara ────────────────────
    // La cámara va DETRÁS del jugador (en Z negativo)
    // y un poco arriba (en Y). Mira hacia adelante con tiltAngle.
    Vector3 GetDesiredPosition(Vector3 pivot)
    {
        return new Vector3(
            pivot.x,
            pivot.y + height,
            pivot.z - currentDistance
        );
    }

    // ── Seguimiento suave ─────────────────────────────────
    void FollowTarget()
    {
        Vector3 tPos = target.position;

        // Suavizar XZ y Y por separado para evitar saltos
        Vector3 targetXZ = new Vector3(tPos.x, 0f, tPos.z) + lookAheadOffset;
        smoothXZ = Vector3.SmoothDamp(smoothXZ, targetXZ, ref xzVelocity, followSmoothing);
        smoothY = Mathf.SmoothDamp(smoothY, tPos.y, ref yVelocity, verticalSmoothing);

        Vector3 pivot = new Vector3(smoothXZ.x, smoothY, smoothXZ.z);

        if (useBounds)
        {
            pivot.x = Mathf.Clamp(pivot.x, boundsMinX, boundsMaxX);
            pivot.z = Mathf.Clamp(pivot.z, boundsMinZ, boundsMaxZ);
        }

        Vector3 desired = GetDesiredPosition(pivot) + shakeOffset;
        transform.position = desired;

        // Rotación fija — siempre mirando ligeramente hacia abajo
        transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
    }

    // ── Zoom ──────────────────────────────────────────────
    void HandleZoom()
    {
        if (!enableScrollZoom) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            targetDistance = Mathf.Clamp(targetDistance - scroll * scrollZoomSpeed, minDistance, maxDistance);

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distVelocity, zoomSmoothing);
        distance = currentDistance;
    }

    // ── Look-Ahead ────────────────────────────────────────
    void HandleLookAhead()
    {
        Vector3 ahead = Vector3.zero;

        if (player != null && player.IsMoving)
        {
            Vector2 dir = player.MoveDirection;
            ahead = new Vector3(dir.x * lookAheadX, 0f, dir.y * lookAheadZ);
        }

        lookAheadOffset = Vector3.SmoothDamp(lookAheadOffset, ahead, ref lookAheadVel, lookAheadSmoothing);
    }

    // ── FOV ───────────────────────────────────────────────
    void HandleFOV()
    {
        float targetFOV = (player != null && player.IsRunning && player.IsMoving) ? runFOV : defaultFOV;
        currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref fovVelocity, fovSmoothing);
        cam.fieldOfView = currentFOV;
    }

    // ── Shake ─────────────────────────────────────────────
    void HandleShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float t = shakeTimer / shakeDuration;
            shakeOffset = Random.insideUnitSphere * shakeMag * t;
            shakeOffset.y = 0f;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    public void Shake(float magnitude, float duration)
    {
        shakeMag = magnitude;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    // ── Gizmos ────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position, 0.3f);
            Gizmos.DrawLine(transform.position, target.position);
        }

        if (useBounds)
        {
            Gizmos.color = Color.cyan;
            Vector3 c = new Vector3((boundsMinX + boundsMaxX) / 2f, 0f, (boundsMinZ + boundsMaxZ) / 2f);
            Vector3 s = new Vector3(boundsMaxX - boundsMinX, 1f, boundsMaxZ - boundsMinZ);
            Gizmos.DrawWireCube(c, s);
        }
    }
}