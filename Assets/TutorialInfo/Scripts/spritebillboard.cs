using UnityEngine;

// ============================================================
//  SpriteBillboard.cs
//  Hace que el sprite 2D SIEMPRE mire a la cámara principal,
//  dando el efecto visual de Eastward (personaje plano en mundo 3D).
//
//  SETUP:
//  1. Adjunta este script al GameObject "Sprite" (hijo del jugador)
//  2. Ese Sprite tiene un SpriteRenderer
//  3. Listo — rotará automáticamente para mirar a la cámara
//
//  MODOS:
//  - Full:       mira completamente a la cámara (esfera)
//  - YAxisOnly:  solo rota en Y (recomendado para personajes)
//  - Locked:     no rota (para sombras proyectadas al suelo)
// ============================================================

public class SpriteBillboard : MonoBehaviour
{
    public enum BillboardMode { Full, YAxisOnly, Locked }

    [Header("=== Configuración ===")]
    public BillboardMode mode = BillboardMode.YAxisOnly;

    [Tooltip("Si true, el billboard se actualiza en LateUpdate (recomendado)")]
    public bool lateUpdate = true;

    [Tooltip("Compensar el ángulo de inclinación de la cámara para que el sprite no se vea ladeado")]
    public bool compensateCameraPitch = true;

    private Camera mainCam;
    private Transform camTransform;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null) camTransform = mainCam.transform;
    }

    void Update()
    {
        if (!lateUpdate) ApplyBillboard();
    }

    void LateUpdate()
    {
        if (lateUpdate) ApplyBillboard();
    }

    void ApplyBillboard()
    {
        if (camTransform == null)
        {
            mainCam = Camera.main;
            if (mainCam != null) camTransform = mainCam.transform;
            return;
        }

        switch (mode)
        {
            case BillboardMode.Full:
                transform.rotation = camTransform.rotation;
                break;

            case BillboardMode.YAxisOnly:
                // Solo rota en Y para que el sprite siempre "mire" a la cámara
                // pero no se incline hacia arriba/abajo
                Vector3 lookDir = camTransform.position - transform.position;
                lookDir.y = 0f;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(-lookDir);

                // Compensar el pitch de la cámara para que el sprite esté siempre vertical
                if (compensateCameraPitch)
                {
                    Vector3 euler = transform.eulerAngles;
                    euler.x = 0f;
                    transform.eulerAngles = euler;
                }
                break;

            case BillboardMode.Locked:
                // No hace nada — útil para sombras
                break;
        }
    }
}

// ============================================================
//  SpriteDepthSorter.cs
//  Ordena el rendering de sprites según su posición Z en el mundo,
//  para que los personajes aparezcan correctamente detrás/delante
//  de edificios y objetos 3D.
//
//  SETUP:
//  1. Adjunta al mismo GameObject "Sprite"
//  2. Asegúrate que tu SpriteRenderer usa "Sorting Layer" correcto
// ============================================================

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteDepthSorter : MonoBehaviour
{
    [Header("=== Sorting ===")]
    [Tooltip("Multiplicador: cuánto afecta Z al orden de dibujado")]
    public float sortingScaleFactor = 100f;

    [Tooltip("Offset base del sorting order")]
    public int sortingOrderBase = 0;

    private SpriteRenderer sr;
    private Transform rootTransform; // raíz del personaje (con la Y del mundo real)

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // Usamos el transform raíz del personaje (padre de este sprite)
        rootTransform = transform.parent != null ? transform.parent : transform;
    }

    void LateUpdate()
    {
        // El sorting order se calcula inversamente a la posición Z
        // → los objetos más "al frente" (Z alto) se dibujan encima
        sr.sortingOrder = sortingOrderBase - (int)(rootTransform.position.z * sortingScaleFactor);
    }
}