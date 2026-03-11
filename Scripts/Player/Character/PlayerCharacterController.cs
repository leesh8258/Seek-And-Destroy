using System.Collections;
using UnityEngine;
using VInspector;

public class PlayerCharacterController : MonoBehaviour
{
    private readonly int HitFlashId = Shader.PropertyToID("_HitFlash");

    [Header("Visual Anchor")]
    [SerializeField] private Transform visualAnchor;

    [Header("Hit Flash")]
    [SerializeField] private float hitFlashDuration = 0.2f;

    [Header("Runtime Info")]
    [SerializeField, ReadOnly] private CharacterSO currentCharacterSO;
    [SerializeField, ReadOnly] private CharacterView characterView;
    [SerializeField, ReadOnly] private GameObject characterInstance;
    [SerializeField, ReadOnly] private Animator characterAnimator;
    [SerializeField, ReadOnly] private FovVisibilityTarget visibilityTarget;

    private MaterialPropertyBlock hitFlashPropertyBlock;
    private Coroutine hitFlashCoroutine;

    public CharacterView CharacterView => characterView;
    public Animator CharacterAnimator => characterAnimator;
    public CharacterSO CurrentCharacterSO => currentCharacterSO;
    public FovVisibilityTarget VisibilityTarget => visibilityTarget;

    private void Awake()
    {
        hitFlashPropertyBlock = new MaterialPropertyBlock();
        visibilityTarget = GetComponent<FovVisibilityTarget>();
    }

    private void OnDestroy()
    {
        StopHitFlash();
        ApplyHitFlashValue(0f);
    }

    public void Initialize(int characterId)
    {
        if (characterInstance != null)
        {
            Destroy(characterInstance);
            characterInstance = null;
            characterAnimator = null;
            characterView = null;
        }

        if (!DBManager.Instance.TryGet<CharacterSO>(characterId, out CharacterSO characterSO))
        {
            Debug.LogError($"[PlayerCharacterController] CharacterSO not found. characterId={characterId}");
            return;
        }

        currentCharacterSO = characterSO;
        if (visualAnchor == null)
        {
            Debug.LogError("[PlayerCharacterController] visualAnchor is null.");
            return;
        }

        GameObject prefab = currentCharacterSO.characterPrefab;
        if (prefab == null)
        {
            Debug.LogError($"[PlayerCharacterController] characterPrefab is null. characterId DB resolved but prefab missing.");
            return;
        }

        characterInstance = Instantiate(prefab, visualAnchor);
        characterInstance.transform.localPosition = Vector3.zero;
        characterInstance.transform.localRotation = Quaternion.identity;
        characterInstance.transform.localScale = Vector3.one;

        characterAnimator = characterInstance.GetComponentInChildren<Animator>();
        characterView = characterInstance.GetComponentInChildren<CharacterView>();

        ApplyHitFlashValue(0f);
    }

    public void PlayHitFlash()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        StopHitFlash();
        ApplyHitFlashValue(1f);
        hitFlashCoroutine = StartCoroutine(HitFlash());
    }

    private void StopHitFlash()
    {
        if (hitFlashCoroutine == null)
        {
            return;
        }

        StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = null;
    }


    private IEnumerator HitFlash()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, hitFlashDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float value = 1f - t;

            ApplyHitFlashValue(value);
            yield return null;
        }

        ApplyHitFlashValue(0f);
        hitFlashCoroutine = null;
    }

    private void ApplyHitFlashValue(float value)
    {
        if (characterView == null)
        {
            return;
        }

        Renderer[] renderers = characterView.BodyRenderers;
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(hitFlashPropertyBlock);
            hitFlashPropertyBlock.SetFloat(HitFlashId, value);
            renderer.SetPropertyBlock(hitFlashPropertyBlock);
        }
    }
}
