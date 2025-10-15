/**
 * ScalpelTipTargetedCutter.cs
 * Attach to the scalpel tip (small trigger collider).
 * While the tip is in contact with the Liver, computes a local cutting plane
 * from the tip position and the liver surface normal, then calls
 * CuttingManager.performCut() at a fixed interval.
 *
 * Requirements:
 *  - CuttingManager in scene (assign in inspector or auto-find).
 *  - Liver object(s) tagged "Liver" or on a dedicated Liver layer.
 *  - Tip collider: isTrigger = true, plus a kinematic Rigidbody on the tip.
 */

using UnityEngine;
using SofaUnity;

[RequireComponent(typeof(Collider))]
public class ScalpelTipTargetedCutter : MonoBehaviour
{
    [Header("References")]
    public CuttingManager cuttingManager;       // If null, auto-find on Start
    public Camera sceneCamera;                  // Optional: for debug rays

    [Header("Target Filter")]
    public LayerMask liverLayer;                // Set to Liver layer; else leave default and rely on tag
    public string liverTag = "Liver";           // Tag check fallback

    [Header("Cut Geometry")]
    [Tooltip("Half-width on the surface from the tip to each side (meters).")]
    public float halfWidth = 0.004f;            // 4 mm to each side (total 8 mm)
    [Tooltip("Cut depth along inward normal (meters).")]
    public float cutDepth = 0.003f;             // 3 mm depth
    [Tooltip("Time between cuts while in contact (seconds).")]
    public float cutInterval = 1.0f;

    [Header("Contact & Raycast")]
    [Tooltip("How far to raycast from tip into the liver to fetch a precise normal.")]
    public float rayDistance = 0.02f;           // 2 cm is plenty
    [Tooltip("Project the scalpel forward onto the surface; if too small, fallback to tip right.")]
    public float minTangentMagnitude = 1e-4f;

    // runtime
    bool inContact = false;
    Collider liverCollider = null;
    float nextCutTime = 0f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        if (cuttingManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            cuttingManager = Object.FindFirstObjectByType<CuttingManager>();
#else
            cuttingManager = Object.FindObjectOfType<CuttingManager>();
#endif
        }
        if (cuttingManager == null)
            Debug.LogError("[ScalpelTipTargetedCutter] CuttingManager not found in scene.");

        // Tip rigidbody kinematic olsun ki trigger stabil çalışsın
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsLiver(other))
        {
            inContact = true;
            liverCollider = other;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other == liverCollider)
        {
            inContact = false;
            liverCollider = null;
        }
    }

    void Update()
    {
        if (!inContact || cuttingManager == null) return;

        if (Time.time >= nextCutTime)
        {
            if (TryComputeCut(out Vector3 A, out Vector3 B, out Vector3 inwardDir))
            {
                // CuttingManager senin projendeki public property’leri expose ediyor:
                cuttingManager.CutPointA   = A;
                cuttingManager.CutPointB   = B;
                cuttingManager.CutDirection = inwardDir;   // içeri doğru normal
                cuttingManager.CutDepth     = cutDepth;

                cuttingManager.performCut();
                nextCutTime = Time.time + cutInterval;
            }
        }
    }

    bool TryComputeCut(out Vector3 A, out Vector3 B, out Vector3 inwardDir)
    {
        A = B = inwardDir = Vector3.zero;
        if (liverCollider == null) return false;

        // 1) Tip konumu
        Vector3 tip = transform.position;

        // 2) Yüzey normalini bulmak için raycast:
        //    Ray’i tipten ileri doğru atıp (transform.forward), layer ile sınırlıyoruz.
        Ray ray = new Ray(tip, transform.forward);
        RaycastHit hit;

        bool hitOk = (liverLayer.value != 0)
            ? Physics.Raycast(ray, out hit, rayDistance, liverLayer, QueryTriggerInteraction.Ignore)
            : Physics.Raycast(ray, out hit, rayDistance, ~0, QueryTriggerInteraction.Ignore);

        // Eğer ileri doğru ray bulamadıysa, geri/yan dene (doku ile temasın yönü farklı olabilir)
        if (!hitOk)
        {
            // Küçük bir fallback: en yakın noktayı al, sonra oraya doğru ray at
            Vector3 closest = liverCollider.ClosestPoint(tip);
            Vector3 dirToClosest = (closest - tip).normalized;
            hitOk = (liverLayer.value != 0)
                ? Physics.Raycast(tip, dirToClosest, out hit, rayDistance, liverLayer, QueryTriggerInteraction.Ignore)
                : Physics.Raycast(tip, dirToClosest, out hit, rayDistance, ~0, QueryTriggerInteraction.Ignore);
        }

        if (!hitOk) return false;

        // 3) Yüzey normali (dışa doğru)
        Vector3 n = hit.normal.normalized;

        // 4) Kesim yönü: neşter yönünü yüzeye projekte et (tanjant)
        Vector3 forwardProj = Vector3.ProjectOnPlane(transform.forward, n);
        if (forwardProj.sqrMagnitude < minTangentMagnitude)
        {
            // ileri çok diktir; sağ ekseni projekte et
            forwardProj = Vector3.ProjectOnPlane(transform.right, n);
        }
        if (forwardProj.sqrMagnitude < minTangentMagnitude)
        {
            // hâlâ küçükse başarısız say
            return false;
        }
        Vector3 t = forwardProj.normalized; // yüzey üzerindeki çizgi yönü

        // 5) A ve B: tip merkezli, yüzey üzerinde iki nokta (soldan/sağdan)
        A = hit.point - t * halfWidth;
        B = hit.point + t * halfWidth;

        // 6) C yönü (CutDirection): içeri doğru normal
        inwardDir = (-n).normalized;

#if UNITY_EDITOR
        if (sceneCamera != null)
        {
            Debug.DrawLine(hit.point, hit.point + n * 0.01f, Color.green, 0.1f);        // normal
            Debug.DrawLine(hit.point, hit.point + t * 0.01f, Color.cyan, 0.1f);         // tangent
            Debug.DrawLine(A, B, Color.magenta, 0.1f);                                  // A-B kenarı
        }
#endif
        return true;
    }

    bool IsLiver(Collider col)
    {
        if (!string.IsNullOrEmpty(liverTag) && col.CompareTag(liverTag))
            return true;

        if (liverLayer.value != 0 && ((1 << col.gameObject.layer) & liverLayer.value) != 0)
            return true;

        // isim fallback’i (son çare)
        return col.name.ToLower().Contains("liver");
    }
}
