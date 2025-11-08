using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FeedbackStitch : MonoBehaviour
{
    public GameObject feedbackTextClose, feedbackTextNoInjection, feedbackNotEven;
    public Transform feedbackTransform;
    public GameObject endText;

    public AudioSource audioSource;
    public AudioClip perfectSound;
    public AudioClip noInjectionSound;
    public float soundGap = 0.02f;

    private readonly Queue<AudioClip> _soundQueue = new Queue<AudioClip>();
    private bool _isPlaying = false;

    public void StitchClose()
    {
        var text= Instantiate(feedbackTextClose, feedbackTransform);
        text.GetComponent<TMP_Text>().text = "Stitches are close!";
    }
    public void StitchFailed()
    {
        var text= Instantiate(feedbackNotEven, feedbackTransform);
        text.GetComponent<TMP_Text>().text = "Failed to stitch!";
    }
    
    public void StitchFar()
    {
        var text= Instantiate(feedbackTextClose, feedbackTransform);
        text.GetComponent<TMP_Text>().text = "Stitches are so far";
    }

    public void StitchPerfect()
    {
        var text = Instantiate(feedbackTextClose, feedbackTransform);
        text.GetComponent<TMP_Text>().text = "Perfect stitch!";
        EnqueueSound(perfectSound);
    }

    public void CheckWin()
    {
        if (FindFirstObjectByType<SutureNeedleZController>().Stitches.Count >= 4)
        {
            Debug.Log("Stitches finished");
            FindFirstObjectByType<SutureNeedleZController>().needle.gameObject.SetActive(false);
            endText.SetActive(true);
        }
    }

    public void StitchNoInjection()
    {
        var text= Instantiate(feedbackTextNoInjection, feedbackTransform);
        text.GetComponent<TMP_Text>().text = "Stitched without pain killer injection!";
        EnqueueSound(noInjectionSound);
    }

    private void EnqueueSound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        _soundQueue.Enqueue(clip);
        if (!_isPlaying)
            StartCoroutine(PlayQueuedSounds());
    }

    private IEnumerator PlayQueuedSounds()
    {
        _isPlaying = true;
        while (_soundQueue.Count > 0)
        {
            var clip = _soundQueue.Dequeue();
            audioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length + soundGap);
        }
        _isPlaying = false;
    }
}
