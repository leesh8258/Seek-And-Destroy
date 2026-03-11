using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

public enum SoundType
{
    BGM,
    UI,
    Weapon,
    Character,
    Environment
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Pool")]
    [Range(1, 64), SerializeField] private int sfx2DPoolCount;
    [Range(1, 64), SerializeField] private int sfx3DPoolCount;
    [ReadOnly] private readonly int bgmPoolCount = 2;
    

    [Header("BGM FadeDuration")]
    [Range(0.5f, 5f), SerializeField] float bgmFadeDuration;

    private Transform sfx2DRoot;
    private Transform sfx3DRoot;
    private Transform bgmRoot;

    private readonly List<AudioSource> sfx2DPool = new List<AudioSource>();
    private readonly List<AudioSource> sfx3DPool = new List<AudioSource>();
    private readonly List<AudioSource> bgmPool = new List<AudioSource>();

    private AudioSource currentBgmSource;
    private AudioSource bgmPool_A;
    private AudioSource bgmPool_B;

    private Coroutine bgmFadeCoroutine;

    #region Initialize

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        CreatePoolRoot();
        CreatePool(sfx2DRoot, sfx2DPool, "SFX2D", sfx2DPoolCount);
        CreatePool(sfx3DRoot, sfx3DPool, "SFX3D", sfx3DPoolCount);
        CreatePool(bgmRoot, bgmPool, "BGM", bgmPoolCount);

        bgmPool_A = bgmPool[0];
        bgmPool_B = bgmPool[1];
    }

    private void CreatePoolRoot()
    {
        sfx2DRoot = CreateRoot("SFX2D_Pool");
        sfx3DRoot = CreateRoot("SFX3D_Pool");
        bgmRoot = CreateRoot("BGM_Pool");
    }

    private Transform CreateRoot(string name)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(transform);
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one;
        return gameObject.transform;
    }

    private void CreatePool(Transform root, List<AudioSource> pool, string name, int count)
    {
        pool.Clear();
        if (root == null) return;
        
        for (int i = 0; i < count; i++)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(root);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
            
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            pool.Add(audioSource);
        }
    }

    #endregion

    #region Sound Play / Stop

    public void Play2DSound(SoundSO soundSO)
    {
        if (soundSO == null) return;

        AudioSource audioSource = GetPool(sfx2DPool);
        SoundSourceSettingSO setting = GetSetting(soundSO.soundType);
        ApplySetting(audioSource, setting);

        audioSource.clip = soundSO.soundClip;
        audioSource.transform.localPosition = Vector3.zero;
        audioSource.Play();
    }

    public void Play3DSound(SoundSO soundSO, Vector3 soundPoint, float volumeMultiplier = 1f)
    {
        if (soundSO == null) return;

        AudioSource audioSource = GetPool(sfx3DPool);
        SoundSourceSettingSO setting = GetSetting(soundSO.soundType);
        ApplySetting(audioSource, setting);

        audioSource.volume *= volumeMultiplier;

        audioSource.clip = soundSO.soundClip;
        audioSource.transform.position = soundPoint;
        audioSource.Play();
    }
    
    public void PlaySoundOneShot(AudioSource audioSource, SoundSO soundSO, float volumeMultiplier = 1f)
    {
        if (audioSource == null || soundSO == null) return;

        SoundSourceSettingSO setting = GetSetting(soundSO.soundType);
        ApplySetting(audioSource, setting);
        
        audioSource.volume *= volumeMultiplier;
        AudioClip clip = soundSO.soundClip;

        audioSource.PlayOneShot(clip);
    }

    public void PlayBGM(SoundSO soundSO)
    {
        if (soundSO == null) return;

        SoundSourceSettingSO setting;
        float targetVolume;

        if (currentBgmSource == null)
        {
            currentBgmSource = bgmPool_A;

            setting = GetSetting(soundSO.soundType);
            ApplySetting(currentBgmSource, setting);

            currentBgmSource.clip = soundSO.soundClip;
            currentBgmSource.transform.localPosition = Vector3.zero;
            currentBgmSource.Play();

            return;
        }

        AudioSource previousAudioSource = currentBgmSource;
        AudioSource nextAudioSource = (previousAudioSource == bgmPool_A) ? bgmPool_B : bgmPool_A;

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        setting = GetSetting(soundSO.soundType);
        ApplySetting(nextAudioSource, setting);

        targetVolume = nextAudioSource.volume;

        nextAudioSource.clip = soundSO.soundClip;
        nextAudioSource.transform.localPosition = Vector3.zero;
        nextAudioSource.volume = 0f;
        nextAudioSource.Play();

        bgmFadeCoroutine = StartCoroutine(CrossFadeBGM(previousAudioSource, nextAudioSource, targetVolume, bgmFadeDuration));
    }

    public void StopAll2DSound()
    {
        StopPool(sfx2DPool);
    }

    public void StopAllPlay3DSound()
    {
        StopPool(sfx3DPool);
    }

    public void StopBGM()
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        StopPool(bgmPool);
        currentBgmSource = null;
    }

    #endregion

    #region Setting

    private AudioSource GetPool(List<AudioSource> pool)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            AudioSource audioSource = pool[i];
            if (audioSource == null) continue;

            if (!audioSource.isPlaying)
            {
                return audioSource;
            }
        }

        AudioSource recyclePool = pool[0];
        if (recyclePool.isPlaying)
        {
            recyclePool.Stop();
        }

        return recyclePool;
    } 

    private void StopPool(List<AudioSource> pool)
    {
        if (pool == null) return;
        
        for (int i = 0; i < pool.Count; i++)
        {
            AudioSource audioSource = pool[i];
            if (audioSource == null) continue;

            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    private SoundSourceSettingSO GetSetting(SoundType soundType)
    {
        if (!DBManager.Instance.TryGet<SoundSourceSettingSO>((int)soundType, out SoundSourceSettingSO soundSourceSettingSO)) return null;
        return soundSourceSettingSO;
    }

    private void ApplySetting(AudioSource audioSource, SoundSourceSettingSO setting)
    {
        if (audioSource == null || setting == null) return;

        audioSource.outputAudioMixerGroup = setting.mixerGroup;
        audioSource.loop = setting.isLoop;
        audioSource.volume = setting.volume;
        audioSource.pitch = setting.pitch;

        audioSource.spatialBlend = setting.spatialBlend;
        audioSource.minDistance = setting.minDistance;
        audioSource.maxDistance = setting.maxDistance;
        audioSource.rolloffMode = setting.rolloffMode;
        
        if (audioSource.rolloffMode == AudioRolloffMode.Custom)
        {
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, setting.customRolloff);
        }
    }

    private IEnumerator CrossFadeBGM(AudioSource previousAudioSource, AudioSource nextAudioSource, float targetVolume, float duration)
    {
        float previousVolume = previousAudioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float lerp = Mathf.Clamp01(elapsed / duration);

            previousAudioSource.volume = Mathf.Lerp(previousVolume, 0f, lerp);
            nextAudioSource.volume = Mathf.Lerp(0f, targetVolume, lerp);

            yield return null;
        }

        previousAudioSource.Stop();
        previousAudioSource.clip = null;

        currentBgmSource = nextAudioSource;
        bgmFadeCoroutine = null;
    }

    #endregion
}
