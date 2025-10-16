using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeedleAreaCollider : MonoBehaviour
{
    public FeedbackNeedle feedback;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Needle"))
        {
            feedback.isOnNeedleArea =  true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Needle"))
        {
            feedback.isOnNeedleArea =  false;
        }
    }
}
