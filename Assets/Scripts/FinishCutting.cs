using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishCutting : MonoBehaviour
{
    public GameObject tumor;
    public GameObject finishCutText;
    public void CheckForFinish()
    {
        StartCoroutine(Finish());    
    }

    private IEnumerator Finish()
    {
        yield return new WaitForSeconds(0.1f);
        if (!FindObjectOfType<Highlight>())
        {
            FindObjectOfType<ScalpelMouseController3D>().scalpel.gameObject.SetActive(false);
            FindObjectOfType<ScalpelMouseController3D>().enabled = false;
            tumor.GetComponent<SmoothRise>().StartRise();
            yield return new WaitForSeconds(2);
            finishCutText.SetActive(true);
            yield return new WaitForSeconds(4);
            FindObjectOfType<SceneChanger>().ChangeScene("SurgerySimulationStitch");
        }
    }
}
