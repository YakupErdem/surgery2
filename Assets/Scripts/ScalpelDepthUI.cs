using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT || TEXTMESHPRO_PRESENT
using TMPro;
#endif

public class ScalpelDepthUI : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Scalpel (bistüri) objesinin Transform'u")]
    [SerializeField] private Transform scalpel;

    [Header("Y Limits (ScalpelMouseController3D ile aynı yap)")]
    [SerializeField] private float minY = -0.01f; // En aşağı (=%100)
    [SerializeField] private float maxY =  0.06f; // En yukarı (=%0)

    [Header("UI")]
    [Tooltip("0-100 arası gösterim için Slider (opsiyonel)")]
    [SerializeField] private Slider depthSlider;
    [Tooltip("Yüzde yazacak Text (UnityEngine.UI.Text) - opsiyonel")]
    [SerializeField] private Text uiText;

    #if TMP_PRESENT || TEXTMESHPRO_PRESENT
    [Tooltip("Yüzde yazacak TMP_Text (TextMeshPro) - opsiyonel")]
    [SerializeField] private TMP_Text tmpText;
    #endif

    
    [Header("Format")]
    [Tooltip("Yazı formatı. {0} yüzdelik sayı.")]
    [SerializeField] private string percentFormat = "{0:0}%";

    void Reset()
    {
        // Editor'de ekleyince auto-fill denemesi
        scalpel = transform;
    }

    void Awake()
    {
        if (depthSlider != null)
        {
            depthSlider.minValue = 0f;
            depthSlider.maxValue = 100f;
            depthSlider.wholeNumbers = true;
        }
    }

    void Update()
    {
        if (scalpel == null)
            return;

        // Güvenli: max=min olmasın
        float yTop = maxY;
        float yBot = minY;
        if (Mathf.Approximately(yTop, yBot))
            return;

        float currentY = scalpel.position.y;

        // maxY -> %0, minY -> %100
        float t = Mathf.InverseLerp(yTop, yBot, currentY); // 0..1
        t = Mathf.Clamp01(t);
        float percent = t * 100f;

        if (depthSlider != null)
            depthSlider.value = percent;

        string textOut = string.Format(percentFormat, percent);

        if (uiText != null)
            uiText.text = textOut;

        #if TMP_PRESENT || TEXTMESHPRO_PRESENT
        if (tmpText != null)
            tmpText.text = textOut;
        #endif
    }

    // İstersen runtime'da dışarıdan senkronlamak için:
    public void SetLimits(float newMinY, float newMaxY)
    {
        minY = newMinY;
        maxY = newMaxY;
    }
}
