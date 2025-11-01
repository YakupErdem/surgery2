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
    [Tooltip("UI üstündeyken tamamen donsun mu? (true: hareket + klik durur, false: sadece klikler blok)")]
    [SerializeField] private bool freezeWhileOverUI = true;
    [SerializeField] private bool preferColliderHit = true;
    [SerializeField] private float rayMaxDistance = 5f;

    [Header("Z Motion (local on referencePlane)")]
    [SerializeField] private float minLocalZ = -0.10f;
    [SerializeField] private float maxLocalZ =  0.10f;
    [Tooltip("0=anlık, 10=çok yumuşak")]
    [SerializeField] private float zLerpSpeed = 12f;

    [Header("Stitch Placement")]
    [Tooltip("Instantiate edilecek dikiş prefab’ı (preview + final için)")]
    [SerializeField] private GameObject stitchPrefab;
    [Tooltip("Stitch’lerin toplanacağı parent; boşsa otomatik oluşturulur")]
    [SerializeField] private Transform stitchesParent;
    [Tooltip("Stitch’in X ve Y’si buradan alınır, Z’si kontrolcünün hesapladığı world Z olur")]
    [SerializeField] private Transform stitchAnchorXY;   // örn. dikiş hattının base noktası

    [Header("Training Flow (hold & drag left)")]
    [Tooltip("İğne indiğinde açılacak eğitim UI paneli/objesi")]
    [SerializeField] private GameObject trainingUI;
    [Tooltip("Sürükleme eşiği (piksel): bu kadar sola çekilirse dikiş başarı sayılır")]
    [SerializeField] private float dragPixelsRequired = 60f;
    [Tooltip("Sola başarıyla çekilince world uzayında yapılacak nudge mesafesi (metre)")]
    [SerializeField] private float successNudgeDistance = 0.004f;
    [Tooltip("Nudge animasyonu hızı (sn^-1)")]
    [SerializeField] private float nudgeLerpSpeed = 14f;
    [Tooltip("İsteğe bağlı: sürükleme sırasında hafifçe kaydırılacak görsel (örn. eldeki ip/needle child)")]
    [SerializeField] private Transform dragVisualToNudge;
    [Tooltip("Drop sonrasında kullanıcının basılı tutması için bekleme (sn)")]
    [SerializeField] private float holdGraceSeconds = 0.35f;

    [Tooltip("Drop’tan sonra illâ Mouse0’ı bırakıp tekrar basmasını iste")]
    [SerializeField] private bool requirePressAfterDrop = false;


    [Header("Spacing Rules")]
    [Tooltip("true: hedef aralık + tolerans, false: min/max eşik")]
    [SerializeField] private bool useTargetSpacing = true;
    [Tooltip("Hedef dikiş aralığı (metre)")]
    [SerializeField] private float targetSpacing = 0.015f;
    [Tooltip("Hedef aralık toleransı (±)")]
    [SerializeField] private float spacingTolerance = 0.003f;
    [Tooltip("useTargetSpacing=false iken: çok yakın eşik")]
    [SerializeField] private float minSeparation = 0.012f;
    [Tooltip("useTargetSpacing=false iken: çok uzak eşik")]
    [SerializeField] private float maxSeparation = 0.030f;

    [Header("Needle Drop/Raise")]
    [Tooltip("Dikiş atarken iğnenin ineceği DÜNYA Y seviyesi")]
    [SerializeField] private float stitchDropWorldY = 0.0f;
    [SerializeField] private float dropSpeed = 3.0f;   // sn^-1 (smooth)
    [SerializeField] private float riseSpeed = 3.0f;   // sn^-1 (smooth)
    [SerializeField] private float ySnapEpsilon = 0.0004f;

    [Header("Feedback (opsiyonel)")]
    public FeedbackStitch feedbackStitch;              // dış sınıfın uyarılarını kullanıyorsan

    [Header("Events")]
    public UnityEvent onStitchPlaced;      // başarılı place sonrası
    public UnityEvent onStitchRemoved;     // remove sonrası
    public UnityEvent onStitchTooClose;    // minSeparation ihlali
    public UnityEvent onStitchTooFar;      // maxSeparation ihlali (min/max modunda)
    public UnityEvent onStitchUneven;      // targetSpacing tolerans dışı (hedef modunda)
    public UnityEvent onStitchFailed;      // drag başarısız (bıraktı/threshold yok)

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
    private bool  _busyAnimating;        // drop/place/rise/drag sırasında kilit
    private readonly List<Transform> _stitches = new List<Transform>();
    public IReadOnlyList<Transform> Stitches => _stitches;

    // pending (preview) stitch referansı
    private Transform _pendingStitch;

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
            stitchAnchorXY = new GameObject("StitchAnchorXY (auto)").transform;
            stitchAnchorXY.position = needle.position;
            stitchAnchorXY.rotation = referencePlane.rotation;
        }
        if (!stitchesParent)
        {
            var go = new GameObject("Stitches");
            stitchesParent = go.transform;
        }
        if (trainingUI) trainingUI.SetActive(false);

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

        if (pointerOverUI && freezeWhileOverUI)
            return;

        // 1) Mouse → plane → Z takibi
        if (!pointerOverUI)
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
                StartCoroutine(DropTrainPlaceRiseFlow());

            if (Input.GetMouseButtonDown(1))
                RemoveLastStitch();
        }
    }

    IEnumerator DropTrainPlaceRiseFlow()
{
    _busyAnimating = true;

    // 1) Dikiş world Z sabitle
    float worldZAtNeedle = needle.position.z;

    // 2) DROP
    yield return StartCoroutine(SmoothWorldY(needle, stitchDropWorldY, dropSpeed));

    // 3) TRAINING UI → aç
    if (trainingUI) trainingUI.SetActive(true);

    // 4) PREVIEW STITCH yarat (listeye hemen ekleme)
    _pendingStitch = CreateStitchPreviewAtWorldZ(worldZAtNeedle);

    // 5) Drag’i başlatma koşulları
    //    a) requirePressAfterDrop=true: önce bırakmasını, sonra tekrar basmasını bekle
    //    b) değilse: basılıysa direkt; değilse holdGraceSeconds içinde basmasını bekle
    float startMouseX = 0f;

    if (requirePressAfterDrop)
    {
        // önce elini butondan çeksin
        while (Input.GetMouseButton(0))
            yield return null;

        // sonra tekrar bassın (sonsuz bekleme değil; istersen timeout koyarsın)
        while (!Input.GetMouseButton(0))
            yield return null;

        startMouseX = Input.mousePosition.x;
    }
    else
    {
        if (Input.GetMouseButton(0))
        {
            // zaten basılı → hemen drag başlat
            startMouseX = Input.mousePosition.x;
        }
        else
        {
            // kısa bir grace penceresi tanıyalım
            float t = 0f;
            bool pressed = false;
            while (t < holdGraceSeconds)
            {
                if (Input.GetMouseButton(0))
                {
                    pressed = true;
                    break;
                }
                t += Time.deltaTime;
                yield return null;
            }

            if (!pressed)
            {
                // hiç basmadı → FAIL (erken dönüş yok; UI kapatıp rise yapacağız)
                if (_pendingStitch)
                {
                    Destroy(_pendingStitch.gameObject);
                    _pendingStitch = null;
                }
                if (trainingUI) trainingUI.SetActive(false);
                onStitchFailed?.Invoke();

                // RISE BACK
                yield return StartCoroutine(SmoothWorldY(needle, _cachedWorldY, riseSpeed));
                _busyAnimating = false;
                yield break;
            }

            startMouseX = Input.mousePosition.x;
        }
    }

    // 6) DRAG-LEFT: Mouse0 basılı kaldığı sürece sola çekmeyi izle
    bool success = false;
    while (true)
    {
        // bırakırsa → fail
        if (!Input.GetMouseButton(0))
            break;

        float dx = Input.mousePosition.x - startMouseX; // px
        if (dx <= -dragPixelsRequired)
        {
            success = true;
            break;
        }
        yield return null;
    }

    // 7) SONUÇ
    if (success)
    {
        // küçük nudge (preview + opsiyonel görsel)
        Vector3 dragLeftWorldDir = -referencePlane.right;

        if (_pendingStitch)
            yield return StartCoroutine(NudgeAlong(_pendingStitch, dragLeftWorldDir, successNudgeDistance, nudgeLerpSpeed));

        if (dragVisualToNudge)
            yield return StartCoroutine(NudgeAlong(dragVisualToNudge, dragLeftWorldDir, successNudgeDistance * 0.5f, nudgeLerpSpeed));

        // finalize → listeye ekle + spacing kontrol + event
        FinalizePendingStitch();
    }
    else
    {
        // threshold’a ulaşmadan bıraktı → FAIL
        if (_pendingStitch)
        {
            Destroy(_pendingStitch.gameObject);
            _pendingStitch = null;
        }
        onStitchFailed?.Invoke();
    }

    // 8) TRAINING UI → kapa
    if (trainingUI) trainingUI.SetActive(false);

    // 9) RISE BACK
    yield return StartCoroutine(SmoothWorldY(needle, _cachedWorldY, riseSpeed));

    _busyAnimating = false;
}


    Transform CreateStitchPreviewAtWorldZ(float worldZ)
    {
        Vector3 pos = new Vector3(
            stitchAnchorXY.position.x,
            stitchAnchorXY.position.y,
            worldZ
        );

        if (stitchPrefab)
        {
            var go = Instantiate(stitchPrefab, pos, Quaternion.identity, stitchesParent);
            return go.transform;
        }
        else
        {
            var go = new GameObject("StitchPreview");
            go.transform.SetParent(stitchesParent);
            go.transform.position = pos;
            return go.transform;
        }
    }

    void FinalizePendingStitch()
    {
        if (_pendingStitch == null) return;

        // listeye ekle
        _stitches.Add(_pendingStitch);
        onStitchPlaced?.Invoke();

        // opsiyonel enjeksiyon kontrolü
        if (!FeedbackNeedle.IsNeedleInjected && feedbackStitch != null)
            feedbackStitch.StitchNoInjection();

        // aralık/uzaklık kontrolü (son komşuyla)
        Transform prev = _stitches.Count > 1 ? _stitches[_stitches.Count - 2] : null;
        if (prev != null)
        {
            float d = Vector3.Distance(_pendingStitch.position, prev.position);

            if (useTargetSpacing)
            {
                float dev = Mathf.Abs(d - targetSpacing);
                if (dev > spacingTolerance) onStitchUneven?.Invoke();
            }
            else
            {
                if (d < minSeparation) onStitchTooClose?.Invoke();
                else if (d > maxSeparation) onStitchTooFar?.Invoke();
            }
        }

        // finalize edildi → artık pending değil
        _pendingStitch = null;
    }

    IEnumerator NudgeAlong(Transform t, Vector3 dirWorld, float distance, float speed)
    {
        if (!t) yield break;
        Vector3 start = t.position;
        Vector3 target = start + dirWorld.normalized * distance;

        while (true)
        {
            float k = 1f - Mathf.Exp(-speed * Time.deltaTime);
            t.position = Vector3.Lerp(t.position, target, k);

            if ((t.position - target).sqrMagnitude <= 1e-7f)
            {
                t.position = target;
                break;
            }
            yield return null;
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
        var sr = stitch.GetComponent<SmoothRise>();
        if (sr) sr.StartRise();
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
