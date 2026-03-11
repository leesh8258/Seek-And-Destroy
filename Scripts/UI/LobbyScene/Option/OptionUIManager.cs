using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionUIManager : MonoBehaviour, IMenuInterface
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("UI Slider")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("UI Value")]
    [SerializeField] private TextMeshProUGUI masterValue;
    [SerializeField] private TextMeshProUGUI bgmValue;
    [SerializeField] private TextMeshProUGUI sfxValue;

    [Header("Close Button")]
    [SerializeField] private Button optionCloseButton;

    public event Action RequestCloseOptionUI;

    private const string masterParameter = "Master_Volume";
    private const string bgmParameter = "BGM_Volume";
    private const string sfxParameter = "SFX_Volume";

    private const string PREFS_MASTER_VOLUME = "Prefs_Master_Volume";
    private const string PREFS_BGM_VOLUME = "Prefs_BGM_Volume";
    private const string PREFS_SFX_VOLUME = "Prefs_SFX_Volume";

    private const float DEFAULT_VALUE = 80f;

    private const float SLIDER_MIN = 0f;
    private const float SLIDER_MAX = 100f;

    private const float MIN_DB = -80f;
    private const float MAX_DB = 0f;

    public void Init()
    {
        if (root != null)
        {
            root.SetActive(false);
        }

        SetUpSlider(masterSlider);
        SetUpSlider(bgmSlider);
        SetUpSlider(sfxSlider);

        RegisterSliderEvents();
        
        if (optionCloseButton != null)
        {
            optionCloseButton.onClick.AddListener(() => RequestCloseOptionUI?.Invoke());
        }

        LoadFromPrefs();
        RefreshValueTexts();
        ApplyAll();
    }

    public void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        RefreshValueTexts();
    }

    public void Hide()
    {
        SaveToPrefs();
        ApplyAll();

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    public void Refresh()
    {
        LoadFromPrefs();
        ApplyAll();
    }

    private void SetUpSlider(Slider slider)
    {
        if (slider == null) return;

        slider.minValue = SLIDER_MIN;
        slider.maxValue = SLIDER_MAX;
        slider.wholeNumbers = true;
    }

    private void RegisterSliderEvents()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }
    }

    private void OnMasterSliderChanged(float value)
    {
        UpdateValueText(masterValue, value);
        ApplyMaster();
    }

    private void OnBgmSliderChanged(float value)
    {
        UpdateValueText(bgmValue, value);
        ApplyBgm();
    }

    private void OnSfxSliderChanged(float value)
    {
        UpdateValueText(sfxValue, value);
        ApplySfx();
    }

    private void LoadFromPrefs()
    {
        float masterVolume = PlayerPrefs.GetFloat(PREFS_MASTER_VOLUME, DEFAULT_VALUE);
        float bgmVolume = PlayerPrefs.GetFloat(PREFS_BGM_VOLUME, DEFAULT_VALUE);
        float sfxVolume = PlayerPrefs.GetFloat(PREFS_SFX_VOLUME, DEFAULT_VALUE);

        if (masterSlider != null) masterSlider.SetValueWithoutNotify(Mathf.Clamp(masterVolume, SLIDER_MIN, SLIDER_MAX));
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(Mathf.Clamp(bgmVolume, SLIDER_MIN, SLIDER_MAX));
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(Mathf.Clamp(sfxVolume, SLIDER_MIN, SLIDER_MAX));
    }

    private void SaveToPrefs()
    {
        float masterVolume = masterSlider != null ? masterSlider.value : DEFAULT_VALUE;
        float bgmVolume = bgmSlider != null ? bgmSlider.value : DEFAULT_VALUE;
        float sfxVolume = sfxSlider != null ? sfxSlider.value : DEFAULT_VALUE;

        PlayerPrefs.SetFloat(PREFS_MASTER_VOLUME, Mathf.Clamp(masterVolume, SLIDER_MIN, SLIDER_MAX));
        PlayerPrefs.SetFloat(PREFS_BGM_VOLUME, Mathf.Clamp(bgmVolume, SLIDER_MIN, SLIDER_MAX));
        PlayerPrefs.SetFloat(PREFS_SFX_VOLUME, Mathf.Clamp(sfxVolume, SLIDER_MIN, SLIDER_MAX));
        PlayerPrefs.Save();
    }

    private void ApplyAll()
    {
        ApplyMaster();
        ApplyBgm();
        ApplySfx();
    }

    private void ApplyMaster()
    {
        if (audioMixer == null || masterSlider == null) return;
        audioMixer.SetFloat(masterParameter, SliderToDB(masterSlider.value));
    }

    private void ApplyBgm()
    {
        if (audioMixer == null || bgmSlider == null) return;
        audioMixer.SetFloat(bgmParameter, SliderToDB(bgmSlider.value));
    }

    private void ApplySfx()
    {
        if (audioMixer == null || sfxSlider == null) return;
        audioMixer.SetFloat(sfxParameter, SliderToDB(sfxSlider.value));
    }

    private void RefreshValueTexts()
    {
        if (masterSlider != null) UpdateValueText(masterValue, masterSlider.value);
        if (bgmSlider != null) UpdateValueText(bgmValue, bgmSlider.value);
        if (sfxSlider != null) UpdateValueText(sfxValue, sfxSlider.value);
    }

    private void UpdateValueText(TextMeshProUGUI valueText, float value)
    {
        if (valueText == null) return;
        valueText.text = Mathf.RoundToInt(value).ToString();
    }

    private float SliderToDB(float sliderValue)
    {
        float normalizedValue = Mathf.Clamp01(sliderValue / SLIDER_MAX);
        if (normalizedValue <= 0.001f) return MIN_DB;

        float dbValue = 20f * Mathf.Log10(normalizedValue);
        return Mathf.Clamp(dbValue, MIN_DB, MAX_DB);
    }

}
