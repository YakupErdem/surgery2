using UnityEngine;
using UnityEngine.Events;
/// <summary>
/// Controls the surgical scalpel during the cutting phase. When the player presses
/// and holds the left mouse button the scalpel is lowered to a fixed cutting
/// height and follows the mouse cursor across the surgical field. A line is drawn
/// along the path of the scalpel tip and deviations from the prescribed cut path
/// trigger feedback through the FeedbackManager. When the cut is released the
/// scalpel is returned to its starting position and an event is raised to notify
/// the rest of the system that cutting has completed.
/// </summary>
public class ScalpelController : MonoBehaviour
{
    [Tooltip("Transform representing the tip of the scalpel. This should be a child of the scalpel object and positioned at the blade tip.")]
    public Transform scalpelTip;

    [Tooltip("Amount in world units the scalpel drops when cutting begins.")]
    public float cutHeightOffset = 0.02f;

    [Tooltip("Speed at which the scalpel moves down and up when beginning and ending a cut.")]
    public float verticalMoveSpeed = 5f;

    [Tooltip("Speed multiplier for horizontal movement when following the mouse.")]
    public float followSpeed = 10f;

    [Tooltip("Renderer used to display the cut line as the scalpel moves. Must be assigned in the inspector.")]
    public LineRenderer cutLineRenderer;

    [Tooltip("Guide defining the ideal cut path. Used to calculate deviation.")]
    public CutPathGuide cutGuide;

    [Tooltip("Maximum allowed deviation in world units from the guide before feedback is shown.")]
    public float deviationTolerance = 0.01f;

    [Tooltip("Manager used for displaying warnings and other on‑screen feedback.")]
    public FeedbackManager feedbackManager;

    [Tooltip("Event raised when the player finishes a cut (mouse released).")]
    public UnityEvent onCutComplete;

    // Internal state
    private Vector3 _initialLocalPosition;
    private bool _isCutting;
    private float _targetHeight;

    private void Awake()
    {
        _initialLocalPosition = transform.localPosition;
        _targetHeight = _initialLocalPosition.y - cutHeightOffset;
        if (cutLineRenderer != null)
        {
            cutLineRenderer.positionCount = 0;
        }
    }

    private void Update()
    {
        // Begin cutting on mouse press
        if (Input.GetMouseButtonDown(0))
        {
            StartCut();
        }

        // Follow the mouse while cutting
        if (Input.GetMouseButton(0) && _isCutting)
        {
            FollowMouse();
        }

        // Finish cutting on mouse release
        if (Input.GetMouseButtonUp(0) && _isCutting)
        {
            EndCut();
        }
    }

    /// <summary>
    /// Initiates the cutting process by lowering the scalpel to the cut height and
    /// preparing the line renderer.
    /// </summary>
    private void StartCut()
    {
        _isCutting = true;
        StopAllCoroutines();
        // Lower the scalpel immediately to the target height
        Vector3 pos = transform.localPosition;
        pos.y = _targetHeight;
        transform.localPosition = pos;
        if (cutLineRenderer != null)
        {
            cutLineRenderer.positionCount = 0;
        }
    }

    /// <summary>
    /// Ends the cutting process by raising the scalpel back to its original height and
    /// invoking the onCutComplete event.
    /// </summary>
    private void EndCut()
    {
        _isCutting = false;
        // Raise scalpel back to initial height
        Vector3 pos = transform.localPosition;
        pos.y = _initialLocalPosition.y;
        transform.localPosition = pos;
        // Notify listeners that the cut has finished
        if (onCutComplete != null)
        {
            onCutComplete.Invoke();
        }
    }

    /// <summary>
    /// Moves the scalpel along the plane defined by the cut height under the mouse cursor.
    /// Adds points to the line renderer and checks deviation from the guide.
    /// </summary>
    private void FollowMouse()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        // Create a plane parallel to the XZ plane at the cutting height
        Plane plane = new Plane(Vector3.up, new Vector3(0f, _targetHeight, 0f));
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            // Smoothly move towards the target point to avoid jitter
            transform.position = Vector3.Lerp(transform.position, new Vector3(worldPoint.x, _targetHeight, worldPoint.z), followSpeed * Time.deltaTime);
            // Record the tip position for the cut line
            if (cutLineRenderer != null && scalpelTip != null)
            {
                Vector3 tipPos = scalpelTip.position;
                // Only append if moved enough to avoid excessive points
                int count = cutLineRenderer.positionCount;
                if (count == 0 || Vector3.Distance(cutLineRenderer.GetPosition(count - 1), tipPos) > 0.001f)
                {
                    cutLineRenderer.positionCount = count + 1;
                    cutLineRenderer.SetPosition(count, tipPos);
                }
                // Check deviation from guide
                if (cutGuide != null)
                {
                    float d = cutGuide.GetClosestDistance(tipPos);
                    if (d > deviationTolerance)
                    {
                        feedbackManager?.ShowWarning("Kesim çizgisinin dışına çıktın!");
                    }
                }
            }
        }
    }
}