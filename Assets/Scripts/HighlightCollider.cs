using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightCollider : MonoBehaviour
{
    public Feedback feedback;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Scalpel"))
        {
            feedback.isOnHighlight = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Scalpel"))
        {
            feedback.isOnHighlight = false;
        }
    }
}
