using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiverCollider : MonoBehaviour
{
    public Feedback feedback;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Scalpel"))
        {
            feedback.isOnLiver =  true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Scalpel"))
        {
            feedback.isOnLiver =  false;
        }
    }
}
