using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class FeedbackNeedle : MonoBehaviour
{
    public bool isOnNeedleArea;
    public GameObject feedbackTextNeedle;
    public Transform feedBackTextParent;
    public float feedbackCooldown = 0.5f;
    private bool canFeedBack = true;
    
    private void Update()
    {
        
    }

    public void NeedleFeedback()
    {
        if(!canFeedBack) return;
        var text= Instantiate(feedbackTextNeedle, feedBackTextParent);
        text.GetComponent<TMP_Text>().text = "Inject needle near the cut!";
        StartCoroutine(CoolDown());
    }

    private IEnumerator CoolDown()
    {
        canFeedBack  = false;
        yield return new WaitForSeconds(feedbackCooldown);
        canFeedBack = true;
    }
}
