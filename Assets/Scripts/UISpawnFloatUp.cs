using UnityEngine;
using UnityEngine.UI;

public class UISpawnFloatUp : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float moveUpDistance = 100f;  // ne kadar yükselecek (UI piksel cinsinden)
    [SerializeField] private float duration = 1f;          // ne kadar sürede
    [SerializeField] private float fadeOutTime = 0.5f;     // kaç saniyede kaybolacak

    private RectTransform rect;
    private CanvasGroup group;
    private Vector2 startPos;
    private float timer;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void OnEnable()
    {
        startPos = rect.anchoredPosition;
        timer = 0f;
        group.alpha = 1f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Yukarı doğru hareket
        float t = timer / duration;
        rect.anchoredPosition = startPos + Vector2.up * (moveUpDistance * t);

        // Fade out
        if (timer > duration - fadeOutTime)
        {
            float fadeT = (timer - (duration - fadeOutTime)) / fadeOutTime;
            group.alpha = Mathf.Lerp(1f, 0f, fadeT);
        }

        // Süre dolunca yok et
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}