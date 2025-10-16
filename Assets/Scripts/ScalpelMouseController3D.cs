using UnityEngine;
using UnityEngine.EventSystems;

public class ScalpelMouseController3D : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] public Transform scalpel;        // boşsa = this.transform
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

    [Header("Y Motion (Common)")]
    [SerializeField] private float maxY = 0.06f;
    [SerializeField] private float minY = -0.01f;

    [Header("Y by Mouse (vertical mapping)")]
    [Tooltip("Açıksa Y ekseni mouse dikeyine göre (viewport Y) kontrol edilir.")]
    [SerializeField] public bool yFollowMouse = false;          // İSTERSEN açık bırak, ama scroll kullanacaksan kapat
    [SerializeField] private Vector2 viewportYRange = new Vector2(0.15f, 0.85f);
    [SerializeField] private float yLerpSpeed = 12f;

    [Header("Y by Scroll (NEW)")]
    [SerializeField] private bool useScrollForY = true;         // scroll ile kontrol
    [SerializeField] private float scrollSensitivity = 0.01f;   // her scroll “tık”ında kaç metre oynasın
    [SerializeField] private bool invertScroll = false;         // yön ters olsun mu
    [SerializeField] private float scrollSmoothing = 0f;        // 0=anlık, >0 yumuşatma (örn. 12)

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

        // UI üstündeyken inputları isteğe göre kapat
        bool pointerOverUI = ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // 1) Mouse ray → plane/collider
        if (!pointerOverUI)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            Vector3 worldPoint;
            if (!(preferColliderHit && TryRaycastReferenceCollider(ray, out worldPoint)) &&
                !TryRaycastReferencePlane(ray, out worldPoint))
            {
                if (logOnceIfNoHit && !_warnedNoHit)
                {
                    Debug.LogWarning("[ScalpelMouseController3D] Ray plane'e çarpmadı. Kamera/plane konum/rotasyonu kontrol et.");
                    _warnedNoHit = true;
                }
                // Y yine güncellenecek (scroll vs.)
            }
            else
            {
                // Dünya → local XZ clamp
                Vector3 local = referencePlane.InverseTransformPoint(worldPoint);
                Vector2 half = localAreaSizeXZ * 0.5f;

                local.y = 0f;
                local.x = Mathf.Clamp(local.x, localAreaCenterXZ.x - half.x, localAreaCenterXZ.x + half.x);
                local.z = Mathf.Clamp(local.z, localAreaCenterXZ.y - half.y, localAreaCenterXZ.y + half.y);

                Vector3 snappedWorld = referencePlane.TransformPoint(local);
                scalpel.position = new Vector3(snappedWorld.x, scalpel.position.y, snappedWorld.z);
            }
        }

        // 2) Y hareketi (öncelik: yFollowMouse → scroll)
        float dt = Time.deltaTime;
        if (yFollowMouse)
            ApplyYFromMouse(dt);
        else if (useScrollForY)
            ApplyYFromScroll(dt, pointerOverUI);
        else
            ApplyYNoop(); // değişiklik yok
    }

    // --- Y: Mouse dikeyine göre (opsiyonel) ---
    void ApplyYFromMouse(float dt)
    {
        float vY = cam.ScreenToViewportPoint(Input.mousePosition).y;
        float t = Mathf.InverseLerp(viewportYRange.x, viewportYRange.y, vY);
        float targetY = Mathf.Lerp(minY, maxY, Mathf.Clamp01(t));

        float k = 1f - Mathf.Exp(-yLerpSpeed * dt);
        _curY = Mathf.Lerp(_curY, targetY, k);
        _curY = Mathf.Clamp(_curY, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));

        var p = scalpel.position;
        scalpel.position = new Vector3(p.x, _curY, p.z);
    }

    // --- Y: Scroll ile kontrol (YENİ) ---
    void ApplyYFromScroll(float dt, bool pointerOverUI)
    {
        // UI üstündeyken scroll’u yoksay (istersen bu davranışı değiştir)
        if (pointerOverUI) { ApplyYNoop(); return; }

        float wheel = Input.mouseScrollDelta.y; // pozitif: yukarı, negatif: aşağı
        if (invertScroll) wheel = -wheel;

        // anlık hedef: delta * sensitivity
        float deltaY = wheel * scrollSensitivity;

        if (Mathf.Abs(deltaY) > 0f)
        {
            if (scrollSmoothing > 0f)
            {
                float targetY = Mathf.Clamp(_curY + deltaY, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
                float k = 1f - Mathf.Exp(-scrollSmoothing * dt);
                _curY = Mathf.Lerp(_curY, targetY, k);
            }
            else
            {
                _curY = Mathf.Clamp(_curY + deltaY, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
            }

            var p = scalpel.position;
            scalpel.position = new Vector3(p.x, _curY, p.z);
        }
        else
        {
            // scroll yoksa pozisyon sabit
            ApplyYNoop();
        }
    }

    // --- Y değişmesin ---
    void ApplyYNoop()
    {
        _curY = Mathf.Clamp(scalpel.position.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
        var p = scalpel.position;
        scalpel.position = new Vector3(p.x, _curY, p.z);
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
        Vector3 planeNormal = referencePlane.TransformDirection(Vector3.up);
        Plane plane = new Plane(planeNormal, referencePlane.position);

        if (plane.Raycast(cam.ScreenPointToRay(Input.mousePosition), out float enter))
        {
            hitPoint = cam.ScreenPointToRay(Input.mousePosition).GetPoint(enter);
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
