using UnityEngine;
using UnityEngine.EventSystems;

public class NeedleMouseController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] public Transform needle;         // boşsa = this.transform
    [SerializeField] private Camera cam;              // gerçek oyun kamerası
    [SerializeField] private Transform referencePlane; // SurgeryPlane / LiverSurface (boş obje de olur)

    [Header("Input")]
    [Tooltip("UI üstündeyken mouse takibini kapat")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;

    [Header("Hit Preference")]
    [Tooltip("ReferencePlane üzerinde Collider varsa önce ona raycast dene")]
    [SerializeField] private bool preferColliderHit = true;

    [Header("Local Area (referencePlane local XZ)")]
    [SerializeField] private Vector2 localAreaCenterXZ = Vector2.zero;
    [SerializeField] private Vector2 localAreaSizeXZ   = new Vector2(0.20f, 0.20f);

    [Header("Press Conditions")]
    [Tooltip("Mouse0'a basınca sadece bu true ise basma animasyonu başlar")]
    [SerializeField]
    public FeedbackNeedle feedbackNeedle;

    [Header("Phase 1: Needle world-Y descend")]
    [SerializeField] private float targetNeedleWorldY = 0.0f; // iğnenin ineceği dünya Y
    [SerializeField] private float needleDownSpeed = 2.0f;    // m/sn (smooth Lerp katsayısı gibi kullanılacak)

    [Header("Phase 2: Pressed part local-Y descend")]
    [SerializeField] private Transform pressedPart;           // iğnenin basılan kısmı (child)
    [SerializeField] private float pressedLocalYTarget = -0.003f; // local y’de hedef
    [SerializeField] private float pressedDownSpeed = 3.0f;       // sn^-1 (smooth)

    [Header("Gizmos/Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool logOnceIfNoHit = true;

    // ---- internal ----
    private bool _pressing = false;       // aktif basma animasyonu var mı
    private bool _phase1Done = false;     // iğne dünya Y hedefe indi mi
    private float _cachedY;               // takip sırasında korunacak sabit Y
    private bool  _warnedNoHit;

    void Awake()
    {
        if (!needle) needle = transform;
        if (!cam) cam = Camera.main;
        _cachedY = needle.position.y; // başlangıç Y değerini sabitliyoruz
    }

    void Update()
    {
        if (!needle || !cam || !referencePlane) return;

        // UI üstündeyken input yok say (opsiyonel)
        bool pointerOverUI = ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // Basma tetikleyicisi: Mouse0
        if (Input.GetMouseButtonDown(0) && feedbackNeedle.isOnNeedleArea && !_pressing)
        {
            // Basma animasyonını başlat
            _pressing = true;
            _phase1Done = false;
        }

        if (Input.GetMouseButtonDown(0) && !feedbackNeedle.isOnNeedleArea)
        {
            feedbackNeedle.NeedleFeedback();
        }
        
        // XZ takip (press yoksa)
        if (!_pressing && !pointerOverUI)
        {
            FollowXZOnPlane();
            // Y’yi sabit tut
            var p = needle.position;
            p.y = _cachedY;
            needle.position = p;
        }

        // Basma akışı
        if (_pressing)
        {
            if (!_phase1Done)
            {
                // Faz 1: iğne dünya Y’de targetNeedleWorldY’ye insin (XZ kilitli)
                SmoothNeedleWorldY(targetNeedleWorldY, needleDownSpeed);
                // Hedefe yaklaştı mı?
                if (Mathf.Abs(needle.position.y - targetNeedleWorldY) < 0.0005f)
                {
                    // sabitle
                    var p = needle.position;
                    p.y = targetNeedleWorldY;
                    needle.position = p;

                    _phase1Done = true;
                }
            }
            else
            {
                // Faz 2: pressedPart local Y’si pressedLocalYTarget’a insin
                if (pressedPart != null)
                {
                    Vector3 lp = pressedPart.localPosition;
                    float k = 1f - Mathf.Exp(-pressedDownSpeed * Time.deltaTime);
                    lp.y = Mathf.Lerp(lp.y, pressedLocalYTarget, k);
                    pressedPart.localPosition = lp;

                    if (Mathf.Abs(lp.y - pressedLocalYTarget) < 0.0002f)
                    {
                        lp.y = pressedLocalYTarget;
                        pressedPart.localPosition = lp;
                        // tüm akış bitti
                        _pressing = false;
                        _cachedY = needle.position.y; // yeni Y’yi cache’leyelim
                    }
                }
                else
                {
                    // pressedPart atanmadıysa faz2 atlanır
                    _pressing = false;
                    _cachedY = needle.position.y;
                }
            }
        }
    }

    // --- XZ takibi (Y sabit) ---
    void FollowXZOnPlane()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Vector3 worldPoint;
        if (!(preferColliderHit && TryRaycastReferenceCollider(ray, out worldPoint)) &&
            !TryRaycastReferencePlane(ray, out worldPoint))
        {
            if (logOnceIfNoHit && !_warnedNoHit)
            {
                Debug.LogWarning("[NeedleMouseController] Ray plane'e çarpmadı. Kamera/plane konum/rotasyonunu kontrol et.");
                _warnedNoHit = true;
            }
            return;
        }

        // Dünya → local XZ clamp
        Vector3 local = referencePlane.InverseTransformPoint(worldPoint);
        Vector2 half = localAreaSizeXZ * 0.5f;

        local.y = 0f;
        local.x = Mathf.Clamp(local.x, localAreaCenterXZ.x - half.x, localAreaCenterXZ.x + half.x);
        local.z = Mathf.Clamp(local.z, localAreaCenterXZ.y - half.y, localAreaCenterXZ.y + half.y);

        Vector3 snappedWorld = referencePlane.TransformPoint(local);
        needle.position = new Vector3(snappedWorld.x, needle.position.y, snappedWorld.z); // Y’yi değiştirme
    }

    // --- Dünya Y’yi smooth indir (lerp-easing) ---
    void SmoothNeedleWorldY(float targetY, float speed)
    {
        var p = needle.position;
        float k = 1f - Mathf.Exp(-speed * Time.deltaTime); // frame-rate bağımsız yumuşatma
        p.y = Mathf.Lerp(p.y, targetY, k);
        needle.position = p;
    }

    // ---- Ray yardımcıları ----
    bool TryRaycastReferenceCollider(Ray ray, out Vector3 hitPoint)
    {
        hitPoint = default;
        var col = referencePlane.GetComponent<Collider>();
        if (!col) return false;

        if (col.Raycast(ray, out var hit, Mathf.Infinity))
        {
            hitPoint = hit.point;
            return true;
        }
        return false;
    }

    bool TryRaycastReferencePlane(Ray ray, out Vector3 hitPoint)
    {
        hitPoint = default;
        Vector3 planeNormal = referencePlane.TransformDirection(Vector3.up);
        Plane plane = new Plane(planeNormal, referencePlane.position);

        if (plane.Raycast(ray, out float enter))
        {
            hitPoint = ray.GetPoint(enter);
            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !referencePlane) return;

        Gizmos.color = new Color(0f, 1f, 0.2f, 0.35f);
        Vector3 cLocal = new Vector3(localAreaCenterXZ.x, 0f, localAreaCenterXZ.y);
        Vector3 cWorld = referencePlane.TransformPoint(cLocal);
        Vector3 hx = referencePlane.right   * (localAreaSizeXZ.x * 0.5f);
        Vector3 hz = referencePlane.forward * (localAreaSizeXZ.y * 0.5f);

        Vector3 a = cWorld - hx - hz;
        Vector3 b = cWorld + hx - hz;
        Vector3 d = cWorld - hx + hz;
        Vector3 e = cWorld + hx + hz;

        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, e);
        Gizmos.DrawLine(e, d); Gizmos.DrawLine(d, a);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
        Vector3 n = referencePlane.TransformDirection(Vector3.up) * 0.08f;
        Gizmos.DrawLine(cWorld, cWorld + n);
    }
#endif
}
