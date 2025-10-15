using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Coordinates the cutting procedure. Listens for the completion event
/// raised by the ScalpelController and triggers subsequent actions such as
/// revealing the tumour. Attach this to a suitable GameObject in the scene.
/// </summary>
public class CutManager : MonoBehaviour
{
    [Tooltip("Scalpel controller responsible for detecting cut completion.")]
    public ScalpelController scalpelController;

    [Tooltip("Tumour object hidden inside the organ that should appear after cutting.")]
    public GameObject tumourObject;

    [Tooltip("Optional event invoked when the cut has completed and the tumour has been shown.")]
    public UnityEvent onCutPhaseFinished;

    private void Awake()
    {
        if (scalpelController != null)
        {
            scalpelController.onCutComplete.AddListener(HandleCutFinished);
        }
        if (tumourObject != null)
        {
            tumourObject.SetActive(false);
        }
    }

    private void HandleCutFinished()
    {
        // Reveal the tumour when the cut is finished
        if (tumourObject != null)
        {
            tumourObject.SetActive(true);
        }
        // Notify listeners that the cut phase has ended
        onCutPhaseFinished?.Invoke();
    }
}