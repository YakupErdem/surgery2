using UnityEngine;

public class SmoothRise : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private float targetY = 2f;         // ulaşılacak Y yüksekliği
    [SerializeField] private float speed = 2f;           // çıkış hızı
    [SerializeField] private bool autoStart = true;      // otomatik başlasın mı

    private bool rising = false;

    void Start()
    {
        if (autoStart)
            StartRise();
    }

    void Update()
    {
        if (rising)
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * speed);
            transform.position = pos;

            // hedefe yaklaştıysa dur
            if (Mathf.Abs(pos.y - targetY) < 0.01f)
            {
                pos.y = targetY;
                transform.position = pos;
                rising = false;
            }
        }
    }

    public void StartRise()
    {
        rising = true;
    }

    public void StopRise()
    {
        rising = false;
    }
}