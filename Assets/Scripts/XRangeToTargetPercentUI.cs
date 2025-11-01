using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT || TEXTMESHPRO_PRESENT
using TMPro;
#endif

public class XRangeToTargetPercentUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;   // takip edilecek Transform

    [Header("X Range (içindeyken %100)")]
    [SerializeField] private float rangeMinX = 0.099f;
    [SerializeField] private float rangeMaxX = 0.086f;

    [Header("Falloff")]
    [Tooltip("Uzaklaştıkça yüzde düşüş hızı (ör: 200 => her 0.5 birimde -100%)")]
    [SerializeField] private float multiplier = 200f;
    [Tooltip("0 = anlık. >0 ise çıktı değerine yumuşatma uygular (lerp).")]
    [SerializeField] private float smooth = 0f;

    [Header("UI (opsiyonel)")]
    [SerializeField] private Slider slider;   // 0..100
    [SerializeField] private Text uiText;     // UnityEngine.UI.Text
    #if TMP_PRESENT || TEXTMESHPRO_PRESENT
    [SerializeField] private TMP_Text tmpText;
    #endif
    [SerializeField] private string percentFormat = "{0:0}%";

    float _smoothedPercent;

    void Awake()
    {
        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
        }
        if (rangeMinX > rangeMaxX)
        {
            var t = rangeMinX;
            rangeMinX = rangeMaxX;
            rangeMaxX = t;
        }
        _smoothedPercent = 100f;
    }

    void Update()
    {
        if (!target) return;

        float x = target.position.x;

        // Aralığa olan uzaklık (0 ise içeride)
        float distance = 0f;
        if (x < rangeMinX) distance = rangeMinX - x;
        else if (x > rangeMaxX) distance = x - rangeMaxX;
        else distance = 0f;

        // Lineer düşüş
        float rawPercent = Mathf.Clamp(100f - distance * multiplier, 0f, 100f);

        // Yumuşatma
        float percent = (smooth > 0f)
            ? Mathf.Lerp(_smoothedPercent, rawPercent, 1f - Mathf.Exp(-smooth * Time.deltaTime))
            : rawPercent;

        _smoothedPercent = percent;

        if (slider) slider.value = percent;

        string s = string.Format(percentFormat, percent);
        if (uiText) uiText.text = s;
        #if TMP_PRESENT || TEXTMESHPRO_PRESENT
        if (tmpText) tmpText.text = s;
        #endif
    }

    // Runtime ayar kolaylıkları
    public void SetRange(float minX, float maxX)
    {
        rangeMinX = Mathf.Min(minX, maxX);
        rangeMaxX = Mathf.Max(minX, maxX);
    }

    public void SetMultiplier(float m) => multiplier = m;


    // ---------------------------
    // 🔥 GIZMO (100'lük bölge çizimi)
    // ---------------------------
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (target == null) return;

        float y = target.position.y;
        float z = target.position.z;

        // Alanın Dünya Uzayındaki Noktaları
        Vector3 left = new Vector3(rangeMinX, y, z);
        Vector3 right = new Vector3(rangeMaxX, y, z);

        // Şeffaf kutu
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.25f);
        Vector3 center = new Vector3((rangeMinX + rangeMaxX) * 0.5f, y, z);
        Vector3 size = new Vector3(Mathf.Abs(rangeMaxX - rangeMinX), 0.01f, 0.01f);
        Gizmos.DrawCube(center, size);

        // Kenarları çiz
        Gizmos.color = Color.green;
        Gizmos.DrawLine(left, right);

        // Uçlara küçük işaret koy
        Gizmos.DrawSphere(left, 0.005f);
        Gizmos.DrawSphere(right, 0.005f);
    }
#endif
}
