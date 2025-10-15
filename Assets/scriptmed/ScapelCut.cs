using System;
using System.Collections;
using System.Collections.Generic;
using SofaUnity;
using UnityEngine;

public class ScapelCut : MonoBehaviour
{
    public CuttingManager cuttingManager;
    public GameObject cutPointA, cutPointB, cutPointC;

    private void Start()
    {
        if (!cuttingManager)
        {
            cuttingManager = FindFirstObjectByType<CuttingManager>();
        }

        StartCoroutine(CutUpdate());
    }

    private IEnumerator CutUpdate()
    {
        yield return new WaitForSeconds(0.1f);
        cuttingManager.performCut();
        StartCoroutine(CutUpdate());
    }
}
