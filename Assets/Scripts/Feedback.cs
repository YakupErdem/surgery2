using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Feedback : MonoBehaviour
{
    public GameObject scalpel;
    public float maxDeepY;
    public bool isOnLiver;
    public GameObject feedbackText;
    public Transform feedBackTextParent;
    public float feedbackCooldown = 0.5f;
    private bool canFeedBack = true;
    
    private void Update()
    {
        if(!isOnLiver && !canFeedBack) return;
        if (scalpel.transform.position.y < maxDeepY)
        {
            var text= Instantiate(feedbackText, feedBackTextParent);
            text.GetComponent<TMP_Text>().text = "Too DEEP!";
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
