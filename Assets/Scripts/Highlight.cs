using UnityEngine;

public class Highlight : MonoBehaviour
{
    [SerializeField] private Color hitColor = Color.green;
    [SerializeField] private float holdDuration = 1f;
    [SerializeField] private FinishCutting finishCutting;

    private Renderer rend;
    private Color originalColor;
    private Color greenColor;
    private float holdTimer = 0f;
    private bool completed = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            var color = Color.red;
            color.a = 0.5f;
            rend.material.color = color;
            originalColor = color;
            greenColor =  Color.green;
            greenColor.a = 0.5f;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (completed) return;
        if (other.CompareTag("Scalpel"))
        {
            // Accumulate time
            holdTimer += Time.deltaTime;
            rend.material.color = Color.Lerp(originalColor, greenColor, holdTimer / holdDuration);

            // Check for completion
            if (holdTimer >= holdDuration)
            {
                completed = true; // Prevent repeated triggers
                finishCutting.CheckForFinish();
                Destroy(gameObject);
            }
        }
    }
}