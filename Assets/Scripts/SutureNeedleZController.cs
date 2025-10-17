using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class SutureNeedleZController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] public Transform needle;           // boşsa = this.transform
    [SerializeField] private Camera cam;                 // raycast kamerası
    [SerializeField] private Transform referencePlane;   // local eksen referansı (local Z boyunca hareket)

    [Header("Input")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;
    [SerializeField] private bool preferColliderHit = true;
    [SerializeField] private float rayMaxDistance = 5f;
    
    [Header("Z Motion (local on referencePlane)")]
    [SerializeField] private float minLocalZ = -0.10f;
    [SerializeField] private float maxLocalZ =  0.10f;
    [Tooltip("0=anlık, 10=çok yumuşak")]
    [SerializeField] private float zLerpSpeed = 12f;

    [Header("Stitch Placement")]
    [Tooltip("Instantiate edilecek dikiş prefab’ı")]
    [SerializeField] private GameObject stitchPrefab;
    [Tooltip("Stitch’lerin toplanacağı parent; boşsa otomatik oluşturulur")]
    [SerializeField] private Transform stitchesParent;
    [Tooltip("Stitch’in X ve Y’si buradan alınır, Z’si kontrolcünün hesapladığı world Z olur")]
    [SerializeField] private Transform stitchAnchorXY;   // örn. dikiş hattının base noktası
    [Tooltip("Yeni dikiş, diğerlerine bu mesafeden yakınsa uyarı ver")]
    [SerializeField] private float minSeparation = 0.012f;

    [Header("Needle Drop/Raise")]
    [Tooltip("Dikiş atarken iğnenin ineceği DÜNYA Y seviyesi")]
    [SerializeField] private float stitchDropWorldY = 0.0f;
    [SerializeField] private float dropSpeed = 3.0f;   // sn^-1 (smooth)
    [SerializeField] private float riseSpeed = 3.0f;   // sn^-1 (smooth)
    [SerializeField] private float ySnapEpsilon = 0.0004f;

    public FeedbackStitch feedbackStitch;

    [Header("Events")]
    public UnityEvent onStitchTooClose;                 // Inspector’dan uyarı UI/metod bağla
    public UnityEvent onStitchPlaced;                   // başarılı place sonrası tetiklenir
    public UnityEvent onStitchRemoved;                  // remove sonrası tetiklenir

    [Header("Debug/Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color railColor = new Color(0f, 1f, 0.2f, 0.7f);
    [SerializeField] private bool logOnceIfNoHit = true;

    // ---- internal ----
    private float _fixedLocalX;
    private float _fixedLocalY;          // genelde 0 (plane üzerinde)
    private float _currentLocalZ;        // hedeflenen local Z
    private float _cachedWorldY;         // normal takipte durduğu dünya Y
    private bool  _warnedNoHit;
    private bool  _busyAnimating;        // drop/place/rise sırasında input kilidi
    private readonly List<Transform> _stitches = new List<Transform>();
    public IReadOnlyList<Transform> Stitches => _stitches;

    void Awake()
    {
        if (!needle) needle = transform;
        if (!cam) cam = Camera.main;

        if (!referencePlane)
        {
            Debug.LogError("[SutureNeedleZController] referencePlane atanmalı.");
            enabled = false;
            return;
        }
        if (!stitchAnchorXY)
        {
            // fallback: needle’ın o anki X/Y’sini anchor varsay
            stitchAnchorXY = new GameObject("StitchAnchorXY (auto)").transform;
            stitchAnchorXY.position = new Vector3(needle.position.x, needle.position.y, needle.position.z);
            stitchAnchorXY.rotation = referencePlane.rotation;
        }
        if (!stitchesParent)
        {
            var go = new GameObject("Stitches");
            stitchesParent = go.transform;
        }

        // iğnenin başlangıç local pozunu al → X/Y sabitlenecek, Z serbest
        var local = referencePlane.InverseTransformPoint(needle.position);
        _fixedLocalX = local.x;
        _fixedLocalY = local.y; // genelde 0
        _currentLocalZ = Mathf.Clamp(local.z, minLocalZ, maxLocalZ);
        _cachedWorldY = needle.position.y;
    }

    void Update()
    {
        if (_busyAnimating) return;
        if (!needle || !cam || !referencePlane) return;

        bool pointerOverUI = ignoreWhenPointerOverUI 
                             && EventSystem.current != null 
                             && EventSystem.current.IsPointerOverGameObject();

        // 1) Mouse → plane → Z takibi (UI üstündeyken sadece hareket isterse açılsın)
        if (!pointerOverUI) // hareketi de kapatmak istersen bu şartı kaldır ve üstte return bırak
        {
            if (TryGetLocalZFromMouse(out float zHit))
            {
                float targetZ = Mathf.Clamp(zHit, minLocalZ, maxLocalZ);
                float k = 1f - Mathf.Exp(-zLerpSpeed * Time.deltaTime);
                _currentLocalZ = Mathf.Lerp(_currentLocalZ, targetZ, k);
                ApplyNeedlePosition(keepY: true);
            }
        }

        // 2) Clickler (UI ÜSTÜNDEYKEN ASLA TETİKLENMEZ)
        if (!pointerOverUI)
        {
            if (Input.GetMouseButtonDown(0))
                StartCoroutine(DropPlaceRiseFlow());

            if (Input.GetMouseButtonDown(1))
                RemoveLastStitch();
        }
    }


    IEnumerator DropPlaceRiseFlow()
    {
        _busyAnimating = true;

        // hedef world Z’yi şimdiden hesapla (stitch bu Z’ye yerleşecek)
        float worldZAtNeedle = needle.position.z;

        // ---- DROP ----
        yield return StartCoroutine(SmoothWorldY(needle, stitchDropWorldY, dropSpeed));

        // ---- PLACE ----
        PlaceStitchAtWorldZ(worldZAtNeedle);

        // ---- RISE BACK ----
        yield return StartCoroutine(SmoothWorldY(needle, _cachedWorldY, riseSpeed));

        _busyAnimating = false;
    }

    void PlaceStitchAtWorldZ(float worldZ)
    {
        Vector3 pos;
        // X/Y sabit noktadan, Z bu kontrolcüden
        pos = new Vector3(
            stitchAnchorXY.position.x,
            stitchAnchorXY.position.y,
            worldZ
        );

        Transform mark;
        if (stitchPrefab)
        {
            var go = Instantiate(stitchPrefab, pos, Quaternion.identity, stitchesParent);
            mark = go.transform;
        }
        else
        {
            var go = new GameObject("StitchMark");
            go.transform.SetParent(stitchesParent);
            go.transform.position = pos;
            mark = go.transform;
        }

        _stitches.Add(mark);
        onStitchPlaced?.Invoke();

        if (!FeedbackNeedle.IsNeedleInjected)
        {
            feedbackStitch.StitchNoInjection();
        }
        
        // Yakınlık kontrolü
        for (int i = 0; i < _stitches.Count - 1; i++)
        {
            float d = Vector3.Distance(pos, _stitches[i].position);
            if (d < minSeparation)
            {
                onStitchTooClose?.Invoke();
                // istersen break;
            }
        }
    }

    void RemoveLastStitch()
    {
        int n = _stitches.Count;
        if (n <= 0) return;

        var last = _stitches[n - 1];
        _stitches.RemoveAt(n - 1);
        if (last)
        {
            StartCoroutine(RemoveStitch(last.gameObject));
        }
    }

    IEnumerator RemoveStitch(GameObject stitch)
    {
        stitch.transform.localScale =
            new Vector3(stitch.transform.localScale.x, 0.002711335f, stitch.transform.localScale.z);
        stitch.GetComponent<SmoothRise>().StartRise();
        yield return new WaitForSeconds(1.8f);
        Destroy(stitch.gameObject);
        onStitchRemoved?.Invoke();
    }

    // --- hareket/hesap ---
    bool TryGetLocalZFromMouse(out float localZ)
    {
        localZ = _currentLocalZ;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Önce collider (varsa) sonra düzlem
        if (preferColliderHit && TryRaycastReferenceCollider(ray, out var hitPoint))
        {
            var local = referencePlane.InverseTransformPoint(hitPoint);
            localZ = local.z;
            return true;
        }

        if (TryRaycastReferencePlane(ray, out hitPoint))
        {
            var local = referencePlane.InverseTransformPoint(hitPoint);
            localZ = local.z;
            return true;
        }

        if (logOnceIfNoHit && !_warnedNoHit)
        {
            Debug.LogWarning("[SutureNeedleZController] Ray plane'e çarpmadı. Kamera/plane doğrultularını kontrol et.");
            _warnedNoHit = true;
        }
        return false;
    }

    void ApplyNeedlePosition(bool keepY)
    {
        // X/Y sabit, Z güncel → world’a çevir
        var local = new Vector3(_fixedLocalX, _fixedLocalY, _currentLocalZ);
        var world = referencePlane.TransformPoint(local);
        if (keepY) world.y = _cachedWorldY;
        needle.position = world;
    }

    IEnumerator SmoothWorldY(Transform t, float targetY, float speed)
    {
        while (true)
        {
            var p = t.position;
            float k = 1f - Mathf.Exp(-speed * Time.deltaTime);
            p.y = Mathf.Lerp(p.y, targetY, k);
            t.position = p;

            if (Mathf.Abs(p.y - targetY) <= ySnapEpsilon)
            {
                p.y = targetY;
                t.position = p;
                break;
            }
            yield return null;
        }
    }

    // ---- Ray yardımcıları ----
    bool TryRaycastReferenceCollider(Ray ray, out Vector3 hitPoint)
    {
        hitPoint = default;
        var col = referencePlane.GetComponent<Collider>();
        if (!col) return false;
        if (col.Raycast(ray, out var hit, rayMaxDistance))
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
        if (plane.Raycast(ray, out float enter) && enter <= rayMaxDistance)
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

        Gizmos.color = railColor;

        Vector3 aLocal = new Vector3(_fixedLocalX, _fixedLocalY, minLocalZ);
        Vector3 bLocal = new Vector3(_fixedLocalX, _fixedLocalY, maxLocalZ);

        // Editörde Awake çağrılmadan önce olabilir; fallback
        if (referencePlane == null)
        {
            aLocal = new Vector3(0f, 0f, minLocalZ);
            bLocal = new Vector3(0f, 0f, maxLocalZ);
        }

        Vector3 a = referencePlane.TransformPoint(aLocal);
        Vector3 b = referencePlane.TransformPoint(bLocal);

        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.002f);
        Gizmos.DrawSphere(b, 0.002f);
    }
#endif
}
