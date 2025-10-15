using UnityEngine;
using System.Collections;

public class Highlight : MonoBehaviour
{
    [SerializeField] private Color hitColor = Color.green; // temas rengi
    [SerializeField] private float destroyDelay = 1f;      // yok olma süresi

    private Renderer rend;
    private Color originalColor;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material.color = Color.red;
        if (rend != null)
            originalColor = rend.material.color;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("trigger enter");
        if (other.CompareTag("Scalpel"))
        {
            if (rend != null)
                rend.material.color = hitColor;

            StartCoroutine(DestroyAfterDelay());
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        FindObjectOfType<FinishCutting>().CheckForFinish();
        Destroy(gameObject);
    }
}