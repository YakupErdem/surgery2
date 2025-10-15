using UnityEngine;
using UnityEngine.EventSystems;

public class ScalpelMouseController3D : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform scalpel;        // boşsa = this.transform
    [SerializeField] private Camera cam;               // gerçek oyun kamerası
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

    [Header("Y Motion")]
    [SerializeField] private float maxY = 0.06f;
    [SerializeField] private float minY = -0.01f;
    [SerializeField] private float descendSpeed = 0.05f; // m/s
    [SerializeField] private float ascendSpeed  = 0.08f; // m/s

    [Header("Gizmos/Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool logOnceIfNoHit = true;

    float _curY;
    bool  _warnedNoHit;

    void Awake()
    {
        if (!scalpel) scalpel = transform;
        if (!cam) cam = Camera.main;
        _curY = Mathf.Clamp(scalpel.position.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
    }

    void Update()
    {
        if (!scalpel || !cam || !referencePlane) return;

        if (ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ApplyY(Time.deltaTime);
            return;
        }

        // 1) Mouse ray
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // 2) Önce collider denemesi
        Vector3 worldPoint = new Vector3();
        if (preferColliderHit && TryRaycastReferenceCollider(ray, out worldPoint) == false)
        {
            // 3) Collider yoksa / vurmadıysa plane ile kesiş
            if (!TryRaycastReferencePlane(ray, out worldPoint))
            {
                if (logOnceIfNoHit && !_warnedNoHit)
                {
                    Debug.LogWarning("[ScalpelMouseSnapRobust] Ray plane'e çarpmadı. Kamera doğrultunu ve referencePlane konum/rotasyonunu kontrol et.");
                    _warnedNoHit = true;
                }
                ApplyY(Time.deltaTime);
                return;
            }
        }

        // 4) Dünya → local (plane uzayında XZ)
        Vector3 local = referencePlane.InverseTransformPoint(worldPoint);

        Vector2 half = localAreaSizeXZ * 0.5f;
        bool inside =
            local.x >= localAreaCenterXZ.x - half.x && local.x <= localAreaCenterXZ.x + half.x &&
            local.z >= localAreaCenterXZ.y - half.y && local.z <= localAreaCenterXZ.y + half.y;

        // Düzlem üzerinde kal
        local.y = 0f;

        // İçerideyse birebir snap, dışarıdaysa clamp
        if (!inside)
        {
            local.x = Mathf.Clamp(local.x, localAreaCenterXZ.x - half.x, localAreaCenterXZ.x + half.x);
            local.z = Mathf.Clamp(local.z, localAreaCenterXZ.y - half.y, localAreaCenterXZ.y + half.y);
        }

        Vector3 snappedWorld = referencePlane.TransformPoint(local);

        // XZ anında uygula
        scalpel.position = new Vector3(snappedWorld.x, scalpel.position.y, snappedWorld.z);

        // Y hareketi
        ApplyY(Time.deltaTime);
    }

    void ApplyY(float dt)
    {
        bool downCut = Input.GetMouseButton(0);   // sol tık -> aşağı in
        bool forceUp = Input.GetMouseButton(2);   // orta tık -> yukarı çık

        float targetY;
        float speed;

        if (downCut)
        {
            targetY = minY;
            speed = descendSpeed;
        }
        else if (forceUp)
        {
            targetY = maxY;
            speed = ascendSpeed * 2f; // istersen daha hızlı çıkması için çarpan
        }
        else
        {
            // mouse bırakıldığında sabit kalsın
            targetY = _curY;
            speed = 0f;
        }


        _curY = MoveTowards(_curY, targetY, speed * dt);
        _curY = Mathf.Clamp(_curY, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));

        var p = scalpel.position;
        scalpel.position = new Vector3(p.x, _curY, p.z);
    }

    static float MoveTowards(float current, float target, float maxDelta)
    {
        if (Mathf.Approximately(current, target)) return target;
        return current < target ? Mathf.Min(current + maxDelta, target)
                                : Mathf.Max(current - maxDelta, target);
    }

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

        // Plane normalini dünya uzayına çevir (eğimli plane için şart)
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

        // Alan dikdörtgeni
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

        // Plane normalini çiz
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
        Vector3 n = referencePlane.TransformDirection(Vector3.up) * 0.08f;
        Gizmos.DrawLine(cWorld, cWorld + n);
    }
#endif
}
