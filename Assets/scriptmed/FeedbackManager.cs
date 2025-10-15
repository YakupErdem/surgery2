using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles display of on‑screen feedback messages. Messages are shown for a
/// configurable duration before being cleared automatically. Attach this
/// component to a Canvas and assign a UI Text element in the inspector.
/// </summary>
public class FeedbackManager : MonoBehaviour
{
    [Tooltip("UI Text used to display feedback messages.")]
    public Text feedbackText;

    [Tooltip("Duration in seconds that a feedback message remains visible.")]
    public float messageDuration = 2f;

    private Coroutine _currentRoutine;

    /// <summary>
    /// Show a warning message immediately. If another message is currently
    /// displayed it will be replaced. After the duration expires the text
    /// automatically clears.
    /// </summary>
    public void ShowWarning(string message)
    {
        if (feedbackText == null) return;
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
        }
        feedbackText.text = message;
        _currentRoutine = StartCoroutine(ClearAfterDelay());
    }

    private IEnumerator ClearAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }
        _currentRoutine = null;
    }
}