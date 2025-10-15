using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the suturing phase of the simulation. Allows the user to place
/// stitches along the cut by clicking near predefined target locations. Each
/// stitch joins the two sides of the incision at that point. The spacing
/// between stitches is evaluated and feedback is provided if stitches are too
/// close together or too far apart.
/// </summary>
public class StitchingManager : MonoBehaviour
{
    [Tooltip("Prefab representing a single suture stitch. Should visually join the two sides of the incision.")]
    public GameObject stitchPrefab;

    [Tooltip("Ordered list of target points along the incision where stitches can be placed.")]
    public Transform[] stitchTargets;

    [Tooltip("Allowed minimum distance (in world units) between consecutive stitches.")]
    public float minSpacing = 0.01f;

    [Tooltip("Allowed maximum distance (in world units) between consecutive stitches.")]
    public float maxSpacing = 0.05f;

    [Tooltip("UI manager used to show warnings about stitch placement.")]
    public FeedbackManager feedbackManager;

    // Track which targets have already been sutured
    private bool[] _sutured;

    // Positions where sutures were placed; used to evaluate spacing
    private readonly List<Vector3> _placedSuturePositions = new List<Vector3>();

    private void Awake()
    {
        if (stitchTargets != null)
        {
            _sutured = new bool[stitchTargets.Length];
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceSuture();
        }
    }

    /// <summary>
    /// Attempts to place a suture at the click location if it is near a valid target.
    /// </summary>
    private void TryPlaceSuture()
    {
        Camera cam = Camera.main;
        if (cam == null || stitchTargets == null || stitchTargets.Length == 0) return;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Find nearest target that has not been sutured
            int closestIndex = -1;
            float closestDist = float.MaxValue;
            for (int i = 0; i < stitchTargets.Length; i++)
            {
                if (_sutured[i]) continue;
                float dist = Vector3.Distance(stitchTargets[i].position, hit.point);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }
            // Only accept if the click was sufficiently close to the target (within 1 cm)
            if (closestIndex >= 0 && closestDist <= 0.01f)
            {
                PlaceSutureAt(closestIndex);
            }
        }
    }

    /// <summary>
    /// Places a suture at the specified target index and evaluates spacing.
    /// </summary>
    private void PlaceSutureAt(int index)
    {
        if (_sutured[index]) return;
        Vector3 pos = stitchTargets[index].position;
        Quaternion rot = stitchTargets[index].rotation;
        if (stitchPrefab != null)
        {
            Instantiate(stitchPrefab, pos, rot);
        }
        _sutured[index] = true;
        // Check spacing relative to previous stitches
        _placedSuturePositions.Add(pos);
        if (_placedSuturePositions.Count > 1)
        {
            // Compare last two suture positions
            Vector3 last = _placedSuturePositions[_placedSuturePositions.Count - 1];
            Vector3 prev = _placedSuturePositions[_placedSuturePositions.Count - 2];
            float dist = Vector3.Distance(last, prev);
            if (dist < minSpacing)
            {
                feedbackManager?.ShowWarning("Dikişler birbirine çok yakın!");
            }
            else if (dist > maxSpacing)
            {
                feedbackManager?.ShowWarning("Dikişler arası mesafe çok uzun!");
            }
        }
    }
}