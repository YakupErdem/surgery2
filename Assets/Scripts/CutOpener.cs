using System;
using System.Collections;
using System.Collections.Generic;
using SofaUnity;
using UnityEngine;

public class CutOpener : MonoBehaviour
{
    public float waitTime = 2;
    public SofaLaserModel sofaLaserModel;

    private void Start()
    {
        StartCoroutine(WaitNOpen());
    }

    private IEnumerator WaitNOpen()
    {
        yield return new WaitForSeconds(waitTime);
        sofaLaserModel.ActivateTool = true;
    }
}
