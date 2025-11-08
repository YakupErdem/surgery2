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
    public float feedbackCooldown = 0.5f;

    public AudioSource audioSource;
    public AudioClip highlightSound;
    public AudioClip deepSound;
    public float countdownBetweenFeedbacks = 0.5f;
    public float soundGap = 0.02f;

    private bool canFeedBack = true;
    private readonly Queue<AudioClip> _soundQueue = new Queue<AudioClip>();
    private bool _isPlayingSounds = false;
    
    private void Update()
    {
        if(!isOnLiver || !canFeedBack) return;
        if (!isOnHighlight && scalpel.transform.position.y < cutDeep)
        {
            var text = Instantiate(feedbackTextHighlight, feedBackTextParent);
            EnqueueSound(highlightSound);
            text.GetComponent<TMP_Text>().text = "Don't cut healthy part!";
        }
        if (scalpel.transform.position.y < maxDeepY)
        {
            var text = Instantiate(feedbackTextDeep, feedBackTextParent);
            EnqueueSound(deepSound);
            text.GetComponent<TMP_Text>().text = "Too DEEP!";
            StartCoroutine(CoolDown());
        }
        else if (!isOnHighlight && scalpel.transform.position.y < cutDeep)
        {
            StartCoroutine(CoolDown());
        }
    }

    private IEnumerator CoolDown()
    {
        canFeedBack  = false;
        yield return new WaitForSeconds(countdownBetweenFeedbacks);
        canFeedBack = true;
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
