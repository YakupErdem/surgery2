using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FeedbackStitch : MonoBehaviour
{
    public GameObject feedbackTextClose, feedbackTextNoInjection;
    public Transform feedbackTransform;
    public GameObject endText;

    public void StitchClose()
    {
        var text= Instantiate(feedbackTextClose, feedbackTransform);
        text.GetComponent<TMP_Text>().text = "Stitches are close!";
    }
    
    public void StitchUnEven()
    {
        var text= Instantiate(feedbackTextNoInjection, feedbackTransform);
        text.GetComponent<TMP_Text>().text = "Stitches are not even!";
    }
    
    public void StitchFar()
    {
        var text= Instantiate(feedbackTextClose, feedbackTransform);
        text.GetComponent<TMP_Text>().text = "Stitches are so far";
    }
    
    public void CheckWin()
    {
        if (FindFirstObjectByType<SutureNeedleZController>().Stitches.Count >= 4)
        {
            Debug.Log("Stitches finished");
            FindFirstObjectByType<SutureNeedleZController>().needle.gameObject.SetActive(false);
            endText.SetActive(true);
        }
    }

    public void StitchNoInjection()
    {
        var text= Instantiate(feedbackTextNoInjection, feedbackTransform);
        text.GetComponent<TMP_Text>().text = "Stitched without pain killer injection!";
    }
}
