using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    [HideInInspector] public bool isOnIndicator;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StitchingNeedle"))
        {
            Debug.Log("Needle on indicator");
            isOnIndicator = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("StitchingNeedle"))
        {
            isOnIndicator = false;
        }
    }
}
