using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Feedback : MonoBehaviour
{
    public GameObject scalpel;
    public float maxDeepY, cutDeep;
    public bool isOnLiver, isOnHighlight;
    public GameObject feedbackTextDeep, feedbackTextHighlight;
    public Transform feedBackTextParent;

    public AudioSource audioSource;
    public AudioClip highlightSound;
    public AudioClip deepSound;
    public float soundGap = 0.02f;

    private readonly Queue<AudioClip> _soundQueue = new Queue<AudioClip>();
    private bool _isPlayingSounds = false;
    
    // Track previous states to detect transitions
    private bool wasTooDeep = false;
    private bool wasOnHealthy = false;
    
    private void Update()
    {
        if(!isOnLiver) return;
        
        float scalpelY = scalpel.transform.position.y;
        bool isTooDeep = scalpelY < maxDeepY;
        bool isOnHealthy = !isOnHighlight && scalpelY < cutDeep;
        
        // Trigger feedback ONLY on state transition (entering bad state)
        if (isTooDeep && !wasTooDeep)
        {
            var text = Instantiate(feedbackTextDeep, feedBackTextParent);
            EnqueueSound(deepSound);
            text.GetComponent<TMP_Text>().text = "Too DEEP!";
        }
        else if (isOnHealthy && !wasOnHealthy && !isTooDeep) // Don't double-trigger if also too deep
        {
            var text = Instantiate(feedbackTextHighlight, feedBackTextParent);
            EnqueueSound(highlightSound);
            text.GetComponent<TMP_Text>().text = "Don't cut healthy part!";
        }
        
        // Update state tracking
        wasTooDeep = isTooDeep;
        wasOnHealthy = isOnHealthy;
    }

    private void EnqueueSound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        _soundQueue.Enqueue(clip);
        if (!_isPlayingSounds)
        {
            StartCoroutine(PlayQueuedSounds());
        }
    }

    private IEnumerator PlayQueuedSounds()
    {
        _isPlayingSounds = true;
        while (_soundQueue.Count > 0)
        {
            var clip = _soundQueue.Dequeue();
            audioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length + soundGap);
        }
        _isPlayingSounds = false;
    }
}
