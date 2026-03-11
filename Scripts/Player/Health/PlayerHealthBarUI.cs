using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private PlayerHealthController healthController;

    [Header("Bars")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image delayedFillImage;

    [Header("Delayed Bar Timing")]
    [SerializeField] private float delayBeforeFollow = 0.20f;
    [SerializeField] private float delayedFollowDuration = 1.00f;

    [Header("Delayed Bar Curve")]
    [SerializeField] private AnimationCurve delayedFollowCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine waitRoutine;
    private Coroutine followRoutine;
    private Camera uiCamera;

    public void SetCamera(Camera camera)
    {
        uiCamera = camera;
    }

    private void Awake()
    {
        if (healthController != null)
        {
            healthController.HealthChanged += OnHealthChanged;
        }

        ResolveCameraIfNeeded();
    }

    private void OnEnable()
    {
        ResolveCameraIfNeeded();
        SyncImmediate();
    }

    private void OnDestroy()
    {
        if (healthController != null)
        {
            healthController.HealthChanged -= OnHealthChanged;
        }

        StopAllBarRoutines();
    }

    private void LateUpdate()
    {
        ResolveCameraIfNeeded();

        if (uiCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(uiCamera.transform.forward, Vector3.up);
        }
    }

    private void ResolveCameraIfNeeded()
    {
        if (uiCamera != null)
        {
            return;
        }

        uiCamera = Camera.main;
    }

    private void OnHealthChanged(int current, int max)
    {
        float newRatio = GetHealthRatio(current, max);

        float prevHealthRatio = healthFillImage != null ? healthFillImage.fillAmount : newRatio;
        float prevDelayedRatio = delayedFillImage != null ? delayedFillImage.fillAmount : newRatio;

        SetHealthFill(newRatio);

        if (newRatio >= prevHealthRatio)
        {
            StopAllBarRoutines();
            SetDelayedFill(newRatio);
            return;
        }

        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;

            float snappedDelayedRatio = Mathf.Max(prevDelayedRatio, prevHealthRatio);
            SetDelayedFill(snappedDelayedRatio);
        }

        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        waitRoutine = StartCoroutine(CoWaitThenFollow());
    }

    private IEnumerator CoWaitThenFollow()
    {
        if (delayBeforeFollow > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFollow);
        }

        waitRoutine = null;
        followRoutine = StartCoroutine(CoFollowDelayedBar());
    }

    private IEnumerator CoFollowDelayedBar()
    {
        if (healthFillImage == null || delayedFillImage == null)
        {
            followRoutine = null;
            yield break;
        }

        float startRatio = delayedFillImage.fillAmount;
        float targetRatio = healthFillImage.fillAmount;

        if (startRatio <= targetRatio + 0.001f)
        {
            delayedFillImage.fillAmount = targetRatio;
            followRoutine = null;
            yield break;
        }

        float duration = delayedFollowDuration;
        if (duration <= 0f)
        {
            delayedFillImage.fillAmount = targetRatio;
            followRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = delayedFollowCurve.Evaluate(t);
            curveT = Mathf.Clamp01(curveT);

            float next = Mathf.Lerp(startRatio, targetRatio, curveT);
            delayedFillImage.fillAmount = next;

            yield return null;
        }

        delayedFillImage.fillAmount = targetRatio;
        followRoutine = null;
    }

    private void StopAllBarRoutines()
    {
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
            followRoutine = null;
        }
    }

    private float GetHealthRatio(int current, int max)
    {
        if (max <= 0) return 0f;
        return Mathf.Clamp01((float)current / max);
    }

    private void SetHealthFill(float ratio)
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = ratio;
        }
    }

    private void SetDelayedFill(float ratio)
    {
        if (delayedFillImage != null)
        {
            delayedFillImage.fillAmount = ratio;
        }
    }

    private void SyncImmediate()
    {
        if (healthController == null)
        {
            return;
        }

        int current = healthController.CurrentHealthPoint;
        int max = healthController.MaxHealthPoint;

        float ratio = GetHealthRatio(current, max);

        StopAllBarRoutines();
        SetHealthFill(ratio);
        SetDelayedFill(ratio);
    }
}