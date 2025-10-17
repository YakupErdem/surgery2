using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class NeedleMouseController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] public Transform needle;          // boşsa = this.transform
    [SerializeField] private Camera cam;               // gerçek oyun kamerası
    [SerializeField] private Transform referencePlane; // SurgeryPlane / LiverSurface

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
    [SerializeField] public FeedbackNeedle feedbackNeedle; // isOnNeedleArea bilgisini buradan alıyoruz

    [Header("Phase 1: Needle world-Y descend")]
    [SerializeField] private float targetNeedleWorldY = 0.0f; // iğnenin ineceği dünya Y
    [SerializeField] private float needleDownSpeed = 2.0f;    // sn^-1 (smooth katsayısı)

    [Header("Phase 2: Pressed part local-Z descend (NEW)")]
    [SerializeField] private Transform pressedPart;           // iğnenin basılan kısmı (child)
    [SerializeField] private float pressedLocalZTarget = -0.003f; // local Z’de hedef
    [SerializeField] private float pressedForwardSpeed = 3.0f;     // sn^-1 (smooth)

    [Header("Gizmos/Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool logOnceIfNoHit = true;

    public GameObject finishNeedleText;

    // ---- internal ----
    private bool _pressing = false;       // aktif basma animasyonu var mı
    private bool _phase1Done = false;     // iğne dünya Y hedefe indi mi
    private float _cachedY;               // takip sırasında sabit tutulacak Y
    private bool  _warnedNoHit;
    private bool _canMove = true;

    void Awake()
    {
        if (!needle) needle = transform;
        if (!cam) cam = Camera.main;
        _cachedY = needle.position.y; // başlangıç Y’yi sabitliyoruz
    }

    void Update()
{
    if (!_canMove) return;
    if (!needle || !cam || !referencePlane) return;

    // >>> UI üstündeyken HER ŞEYİ kilitle (tıklama + hareket + anim)
    bool pointerOverUI = ignoreWhenPointerOverUI 
                         && EventSystem.current != null 
                         && EventSystem.current.IsPointerOverGameObject();
    if (pointerOverUI)
        return;
    // <<<

    // Mouse0 basma tetikleyicisi
    if (Input.GetMouseButtonDown(0))
    {
        if (feedbackNeedle != null && feedbackNeedle.isOnNeedleArea && !_pressing)
        {
            _pressing = true;
            _phase1Done = false;
        }
        else if (feedbackNeedle != null && !feedbackNeedle.isOnNeedleArea)
        {
            feedbackNeedle.NeedleFeedback();
        }
    }

    // Press yoksa XZ takibi (Y sabit)
    if (!_pressing)
    {
        FollowXZOnPlane();
        var p = needle.position;
        p.y = _cachedY;
        needle.position = p;
    }

    // Press akışı
    if (_pressing)
    {
        if (!_phase1Done)
        {
            SmoothNeedleWorldY(targetNeedleWorldY, needleDownSpeed);

            if (Mathf.Abs(needle.position.y - targetNeedleWorldY) < 0.0005f)
            {
                var p = needle.position;
                p.y = targetNeedleWorldY;
                needle.position = p;
                _phase1Done = true;
            }
        }
        else
        {
            if (pressedPart != null)
            {
                Vector3 lp = pressedPart.localPosition;
                float k = 1f - Mathf.Exp(-pressedForwardSpeed * Time.deltaTime);
                lp.z = Mathf.Lerp(lp.z, pressedLocalZTarget, k);
                pressedPart.localPosition = lp;

                if (Mathf.Abs(lp.z - pressedLocalZTarget) < 0.0002f)
                {
                    lp.z = pressedLocalZTarget;
                    pressedPart.localPosition = lp;
                    _pressing = false;
                    _cachedY = needle.position.y;
                    StartCoroutine(Finish());
                }
            }
            else
            {
                _pressing = false;
                _cachedY = needle.position.y;
            }
        }
    }
}


    private IEnumerator Finish()
    {
        _canMove  = false;
        needle.gameObject.GetComponent<SmoothRise>().StartRise();
        yield return new WaitForSeconds(2);
        needle.gameObject.SetActive(false);
        finishNeedleText.SetActive(true);
        FeedbackNeedle.IsNeedleInjected = true;
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
        needle.position = new Vector3(snappedWorld.x, needle.position.y, snappedWorld.z); // Y sabit
    }

    // --- Dünya Y’yi smooth indir ---
    void SmoothNeedleWorldY(float targetY, float speed)
    {
        var p = needle.position;
        float k = 1f - Mathf.Exp(-speed * Time.deltaTime);
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
