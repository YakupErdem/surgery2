using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishCutting : MonoBehaviour
{
    public GameObject tumor;
    public GameObject finishCutText;
    public void CheckForFinish()
    {
        if (!FindObjectOfType<Highlight>())
        {
            StartCoroutine(Finish());    
        }
    }

    private IEnumerator Finish()
    {
        FindObjectOfType<ScalpelMouseController3D>().scalpel.gameObject.SetActive(false);
        FindObjectOfType<ScalpelMouseController3D>().enabled = false;
        tumor.GetComponent<SmoothRise>().StartRise();
        yield return new WaitForSeconds(2);
        finishCutText.SetActive(true);
        yield return new WaitForSeconds(2);
    }
}
