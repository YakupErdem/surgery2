using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterWithPause : MonoBehaviour
{
    public TMP_Text uiText;
    public float typeSpeed = 0.05f;

    public void PlayText(string textToWrite)
    {
        StopAllCoroutines();
        StartCoroutine(TypeRoutine(textToWrite));
    }

    private IEnumerator TypeRoutine(string fullText)
    {
        uiText.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            // Eğer "(" görürse --> cooldown modu
            if (fullText[i] == '(')
            {
                string numberBuffer = "";
                i++; 

                while (i < fullText.Length && fullText[i] != ')')
                {
                    numberBuffer += fullText[i];
                    i++;
                }

                // numberBuffer = bekleme süresi
                if (float.TryParse(numberBuffer, out float waitTime))
                    yield return new WaitForSeconds(waitTime);

                continue;  // cooldowndan sonra harf basmaya devam
            }

            // Normal harf bas
            uiText.text += fullText[i];
            yield return new WaitForSeconds(typeSpeed);
        }
    }
}