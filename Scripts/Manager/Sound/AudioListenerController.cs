using UnityEngine;
using VInspector;

[RequireComponent(typeof(AudioListener))]
public class AudioListenerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, ReadOnly] private Transform followTarget;
    [SerializeField, ReadOnly] private Transform screenReference;
    [SerializeField, ReadOnly] private AudioListener cachedAudioListener;

    [Header("Position")]
    [SerializeField] private Vector3 followOffset = Vector3.zero;

    private void Awake()
    {
        cachedAudioListener = GetComponent<AudioListener>();

        if (cachedAudioListener != null)
        {
            cachedAudioListener.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;
        if (screenReference == null) return;

        transform.position = followTarget.position + followOffset;
        UpdateScreenAlignedRotation();
    }

    public void Bind(Transform target, Transform cameraTransform)
    {
        followTarget = target;
        screenReference = cameraTransform;

        if (cachedAudioListener != null)
        {
            cachedAudioListener.enabled = true;
        }

        if (followTarget == null || screenReference == null) return;

        transform.position = followTarget.position + followOffset;
        UpdateScreenAlignedRotation();
    }

    public void Unbind()
    {
        followTarget = null;
        screenReference = null;

        if (cachedAudioListener != null)
        {
            cachedAudioListener.enabled = false;
        }
    }

    private void UpdateScreenAlignedRotation()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(screenReference.forward, Vector3.up);

        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.forward;
        }

        transform.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }
}