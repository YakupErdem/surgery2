using UnityEngine;

public class MenuPopper : MonoBehaviour
{
    [Header("Pop Ayarları")]
    public float popDuration = 0.25f;
    public float popScale = 1.15f;

    /// <summary>
    /// Bu methoda bir menü GameObject atarsın, 
    /// kankito menüyü poplaya poplaya açar :)
    /// </summary>
    public void OpenMenu(GameObject menu)
    {
        if (menu == null) return;

        menu.SetActive(true);
        menu.transform.localScale = Vector3.zero;

        // Animasyon başlasın!
        StartCoroutine(PopRoutine(menu.transform));
    }

    private System.Collections.IEnumerator PopRoutine(Transform tr)
    {
        float t = 0f;

        // İlk overshoot
        Vector3 start = Vector3.zero;
        Vector3 mid = Vector3.one * popScale;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            float p = t / popDuration;
            tr.localScale = Vector3.Lerp(start, mid, p);
            yield return null;
        }

        // Overshoot’tan normale dön
        t = 0f;
        Vector3 end = Vector3.one;

        while (t < popDuration * 0.6f)
        {
            t += Time.deltaTime;
            float p = t / (popDuration * 0.6f);
            tr.localScale = Vector3.Lerp(mid, end, p);
            yield return null;
        }

        tr.localScale = Vector3.one;
    }
}