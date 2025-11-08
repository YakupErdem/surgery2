using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StitchIndicatorSequence : MonoBehaviour
{
    [Header("Scene Indicators (order)")]
    [Tooltip("Sahnedeki indikatör GameObject'leri. Prefab DEĞİL, direkt sahne referansı.")]
    public List<GameObject> indicators = new List<GameObject>();

    [Header("Options")]
    [Tooltip("Awake'te listedeki tüm indikatörleri kapatır.")]
    public bool deactivateAllOnAwake = true;

    [Tooltip("Begin() çağrılınca ilk indikatörü otomatik aç.")]
    public bool showFirstOnBegin = true;

    [Header("Events")]
    public UnityEvent<int> OnStepChanged;   // aktif index (0-based), -1 ise aktif yok
    public UnityEvent OnSequenceCompleted;  // son eleman geçildikten sonra

    // --- State ---
    private int _currentIndex = -1;
    public GameObject ActiveIndicator => (_currentIndex >= 0 && _currentIndex < indicators.Count) ? indicators[_currentIndex] : null;
    public Transform ActiveIndicatorTransform => ActiveIndicator ? ActiveIndicator.transform : null;

    void Awake()
    {
        if (deactivateAllOnAwake)
            SetAll(false);
    }

    private void Start()
    {
        Begin();
    }

    // Baştan başlat: tümünü kapat, index'i -1 yap, gerekirse ilkini aç
    public void Begin()
    {
        SetAll(false);
        _currentIndex = -1;

        if (showFirstOnBegin && indicators.Count > 0)
            Next();
        else
            OnStepChanged?.Invoke(_currentIndex);
    }

    // Sıradakine geç: eskiyi kapat, yeniyi aç
    public void Next()
    {
        // Eskiyi kapat
        if (IsValidIndex(_currentIndex))
            indicators[_currentIndex].SetActive(false);

        int nextIndex = _currentIndex + 1;

        if (!IsValidIndex(nextIndex))
        {
            // Dizi bitti → aktif yok
            _currentIndex = -1;
            OnStepChanged?.Invoke(_currentIndex);
            OnSequenceCompleted?.Invoke();
            return;
        }

        _currentIndex = nextIndex;
        indicators[_currentIndex]?.SetActive(true);
        OnStepChanged?.Invoke(_currentIndex);
    }

    // Geri gitmek istersen (opsiyonel)
    public void Prev()
    {
        if (IsValidIndex(_currentIndex))
            indicators[_currentIndex].SetActive(false);

        int prevIndex = _currentIndex - 1;
        if (!IsValidIndex(prevIndex))
        {
            _currentIndex = -1;
            OnStepChanged?.Invoke(_currentIndex);
            return;
        }

        _currentIndex = prevIndex;
        indicators[_currentIndex]?.SetActive(true);
        OnStepChanged?.Invoke(_currentIndex);
    }

    // Belirli indexe atla (0-based)
    public void JumpTo(int index)
    {
        if (IsValidIndex(_currentIndex))
            indicators[_currentIndex].SetActive(false);

        if (!IsValidIndex(index))
        {
            _currentIndex = -1;
            OnStepChanged?.Invoke(_currentIndex);
            return;
        }

        _currentIndex = index;
        indicators[_currentIndex]?.SetActive(true);
        OnStepChanged?.Invoke(_currentIndex);
    }

    // Hepsini kapat, index -1
    public void ResetSequence()
    {
        SetAll(false);
        _currentIndex = -1;
        OnStepChanged?.Invoke(_currentIndex);
    }

    public int GetCurrentIndex() => _currentIndex;

    private bool IsValidIndex(int i) => i >= 0 && i < indicators.Count;

    private void SetAll(bool state)
    {
        for (int i = 0; i < indicators.Count; i++)
        {
            if (indicators[i] != null)
                indicators[i].SetActive(state);
        }
    }

#if UNITY_EDITOR
    // Sahnedeki sırayı ve aktif olanı görsel olarak göster
    private void OnDrawGizmosSelected()
    {
        if (indicators == null || indicators.Count == 0) return;

        Vector3? last = null;
        for (int i = 0; i < indicators.Count; i++)
        {
            var go = indicators[i];
            if (!go) continue;

            // Aktif olan sarı, diğerleri camgöbeği
            Gizmos.color = (i == _currentIndex) ? new Color(1f, 0.9f, 0f, 0.85f) : new Color(0f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(go.transform.position, 0.01f);

            if (last.HasValue)
                Gizmos.DrawLine(last.Value, go.transform.position);

            last = go.transform.position;
        }
    }
#endif
}
