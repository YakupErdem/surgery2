using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FinishCutting : MonoBehaviour
{
    public GameObject tumor;
    public GameObject finishCutText;
    private int totalPoints = 4;
    

    public void CheckForFinish()
    {
        Debug.Log("FINISHED: " + totalPoints);
        totalPoints--;
        if(totalPoints<=0) StartCoroutine(Finish());    
    }

    private IEnumerator Finish()
    {
        yield return new WaitForSeconds(0.1f);
        FindObjectOfType<ScalpelMouseController3D>().scalpel.gameObject.SetActive(false);
        FindObjectOfType<ScalpelMouseController3D>().enabled = false;
        tumor.GetComponent<SmoothRise>().StartRise();
        yield return new WaitForSeconds(2);
        finishCutText.SetActive(true);
    }
}
