using UnityEngine;

/// <summary>
/// Defines an ideal cut path as a series of control points in world space. Provides
/// methods for computing the closest distance from an arbitrary position to the
/// path, which can be used to determine deviation during cutting. The path can
/// also be visualised using a LineRenderer when running in the editor or game.
/// </summary>
public class CutPathGuide : MonoBehaviour
{
    [Tooltip("Ordered list of transforms representing the ideal cut line. The scalpel tip should stay close to this path.")]
    public Transform[] pathPoints;

    [Tooltip("Optional renderer used to display the guide line in the scene.")]
    public LineRenderer guideRenderer;

    private void Awake()
    {
        // If a guide renderer is assigned then initialise it using the path points
        if (guideRenderer != null && pathPoints != null && pathPoints.Length > 1)
        {
            guideRenderer.positionCount = pathPoints.Length;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                guideRenderer.SetPosition(i, pathPoints[i].position);
            }
        }
    }

    /// <summary>
    /// Returns the minimal perpendicular distance from the given point to the ideal path.
    /// The path is treated as a polyline connecting each successive pair of points.
    /// </summary>
    /// <param name="point">Point in world space to test.</param>
    /// <returns>Minimum distance to the path in world units.</returns>
    public float GetClosestDistance(Vector3 point)
    {
        if (pathPoints == null || pathPoints.Length < 2)
        {
            return float.MaxValue;
        }
        float minDist = float.MaxValue;
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Vector3 a = pathPoints[i].position;
            Vector3 b = pathPoints[i + 1].position;
            float dist = DistancePointToSegment(point, a, b);
            if (dist < minDist)
            {
                minDist = dist;
            }
        }
        return minDist;
    }

    /// <summary>
    /// Calculates the shortest distance between a point and a line segment.
    /// </summary>
    private static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        float t = Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        Vector3 closestPoint = a + ab * t;
        return Vector3.Distance(p, closestPoint);
    }
}