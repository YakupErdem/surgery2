using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StitchingInstruction : MonoBehaviour
{
    public MenuChanger menuChanger;
    [SerializeField] private TypewriterWithPause _typeWriter;
    [SerializeField] private GameObject audioSourceObject;
    [SerializeField] private GameObject menuChangeCanvas;
    [SerializeField] private string _instructionText;
    [SerializeField] private float _instructionTime;

    private void Start()
    {
        SetInstruction(true);
        StartCoroutine(InstructionSequence());
    }

    public IEnumerator InstructionSequence()
    {
        yield return new WaitForSeconds(0.8f);
        audioSourceObject.SetActive(true);
        _typeWriter.PlayText(_instructionText);
        yield return new WaitForSeconds(_instructionTime);
        audioSourceObject.SetActive(false);
        SetInstruction(false);
    }

    private void SetInstruction(bool c)
    {
        menuChangeCanvas.SetActive(!c);
        if (c)
        {
            menuChanger.OpenMenu(3);
        }
        else
        {
            menuChanger.OpenMenu(2);
        }
    }
}
