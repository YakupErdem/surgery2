using System;
using System.Collections;
using System.Collections.Generic;
using SofaUnity;
using UnityEngine;

public class ScapelCut : MonoBehaviour
{
    public CuttingManager cuttingManager;
    public GameObject cutPointA, cutPointB, cutPointC;
    public bool autoOnStart = false;
    public bool hit;

    private void Start()
    {
        if (!cuttingManager)
        {
            cuttingManager = FindFirstObjectByType<CuttingManager>();
        }

        if(autoOnStart) StartCoroutine(CutUpdate());
    }

    private void Update()
    {
        if (hit)
        {
            hit = false;
            cuttingManager.CutPointA = cutPointA.transform.position;
            cuttingManager.CutPointB = cutPointA.transform.position;
            cuttingManager.CutDirection = cutPointC.transform.position;
            cuttingManager.performCut();
        }
    }

    private IEnumerator CutUpdate()
    {
        yield return new WaitForSeconds(0.1f);
        cuttingManager.CutPointA = cutPointA.transform.position;
        cuttingManager.CutPointB = cutPointA.transform.position;
        cuttingManager.CutDirection = cutPointC.transform.position;
        cuttingManager.performCut();
        StartCoroutine(CutUpdate());
    }
}
