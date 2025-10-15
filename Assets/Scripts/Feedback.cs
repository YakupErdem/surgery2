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
    private bool canFeedBack = true;
    
    private void Update()
    {
        if(!isOnLiver || !canFeedBack) return;
        if (!isOnHighlight && scalpel.transform.position.y < cutDeep)
        {
            var text= Instantiate(feedbackTextHighlight, feedBackTextParent);
            text.GetComponent<TMP_Text>().text = "Don't cut healthy part!";
        }
        if (scalpel.transform.position.y < maxDeepY)
        {
            var text= Instantiate(feedbackTextDeep, feedBackTextParent);
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
        yield return new WaitForSeconds(feedbackCooldown);
        canFeedBack = true;
    }
}
