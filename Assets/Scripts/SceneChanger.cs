using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{

    private float _seconds;
    
    public void ChangeScene(string sceneName)
    {
        StartCoroutine(WaitNChangeScene(sceneName));
    }
    
    public void SetSeconds(float seconds) => _seconds = seconds;

    IEnumerator WaitNChangeScene(string sceneName)
    {
        yield return new WaitForSeconds(_seconds);
        SceneManager.LoadScene(sceneName);
    }
}
