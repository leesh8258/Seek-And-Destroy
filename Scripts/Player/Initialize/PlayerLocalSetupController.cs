using Cinemachine;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(PlayerComponentCache))]
public class PlayerLocalSetupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, ReadOnly] private PlayerComponentCache componentCache;
    [SerializeField, ReadOnly] private Camera gameplayCamera;
    [SerializeField, ReadOnly] private CinemachineVirtualCamera virtualCamera;
    [SerializeField, ReadOnly] private AudioListenerController audioListenerController;

    [Header("Camera Follow")]
    [SerializeField] private Transform cameraFollowTarget;

    public bool IsLocalPlayer
    {
        get
        {
            return componentCache != null
                && componentCache.PhotonView != null
                && componentCache.PhotonView.IsMine;
        }
    }

    private void Awake()
    {
        if (componentCache == null)
        {
            componentCache = GetComponent<PlayerComponentCache>();
        }
    }

    public void SetupLocalPlayer()
    {
        if (componentCache == null || !componentCache.HasAllCoreComponents())
        {
            return;
        }

        if (IsLocalPlayer)
        {
            SetupOwnedPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }
    }

    public void RefreshLocalBindingsAfterWeaponChange()
    {
        if (!IsLocalPlayer) return;
        if (componentCache == null) return;

        ApplyOwnedBindings();
    }

    private void SetupOwnedPlayer()
    {
        if (componentCache.InputController != null)
        {
            componentCache.InputController.enabled = true;
        }

        if (componentCache.UpdateController != null)
        {
            componentCache.UpdateController.enabled = true;
        }

        if (componentCache.FovController != null)
        {
            componentCache.FovController.enabled = true;
        }

        ApplyOwnedBindings();
    }

    private void SetupRemotePlayer()
    {
        if (componentCache.InputController != null)
        {
            componentCache.InputController.enabled = false;
        }

        if (componentCache.UpdateController != null)
        {
            componentCache.UpdateController.enabled = false;
        }

        if (componentCache.FovController != null)
        {
            componentCache.FovController.enabled = false;
        }
    }

    private void ApplyOwnedBindings()
    {
        Camera camera = ResolveGameplayCamera();
        Transform weaponAnchor = componentCache.WeaponController != null ? componentCache.WeaponController.WeaponAnchor : null;
        FovVisibilityTarget selfTarget = componentCache.CharacterController != null ? componentCache.CharacterController.VisibilityTarget : null;

        if (camera != null && componentCache.MoveController != null)
        {
            componentCache.MoveController.SetCamera(camera);
        }

        if (camera != null)
        {
            BindHealthBarCamera(camera);
        }

        if (camera != null && weaponAnchor != null && componentCache.AimController != null)
        {
            componentCache.AimController.Initialize(camera, weaponAnchor);
        }

        if (componentCache.FovController != null && componentCache.AimController != null && selfTarget != null)
        {
            componentCache.FovController.Initialize(componentCache.AimController, selfTarget);
        }

        if (WeaponEffectsManager.Instance != null && componentCache.FovController != null)
        {
            WeaponEffectsManager.Instance.SetViewerFovController(componentCache.FovController);
        }

        BindVirtualCamera();
        BindAudioListenerController(camera);
    }

    private void BindHealthBarCamera(Camera camera)
    {
        if (camera == null) return;
        if (componentCache == null || componentCache.CharacterController == null) return;
        if (componentCache.CharacterController.CharacterView == null) return;

        PlayerHealthBarUI[] healthBars = componentCache.CharacterController.CharacterView.GetComponentsInChildren<PlayerHealthBarUI>(true);
        if (healthBars == null || healthBars.Length == 0) return;

        for (int i = 0; i < healthBars.Length; i++)
        {
            PlayerHealthBarUI healthBar = healthBars[i];
            if (healthBar == null) continue;

            healthBar.SetCamera(camera);
        }
    }

    private Camera ResolveGameplayCamera()
    {
        if (gameplayCamera != null)
        {
            return gameplayCamera;
        }

        gameplayCamera = Camera.main;
        return gameplayCamera;
    }

    private void BindVirtualCamera()
    {
        if (!IsLocalPlayer) return;

        if (virtualCamera == null)
        {
            virtualCamera = FindAnyObjectByType<CinemachineVirtualCamera>();
        }

        if (virtualCamera == null) return;

        Transform followTarget = cameraFollowTarget != null ? cameraFollowTarget : transform;
        virtualCamera.Follow = followTarget;
    }

    private void BindAudioListenerController(Camera camera)
    {
        if (!IsLocalPlayer) return;
        if (camera == null) return;

        if (audioListenerController == null)
        {
            audioListenerController = FindAnyObjectByType<AudioListenerController>();
        }

        if (audioListenerController == null) return;

        audioListenerController.Bind(transform, camera.transform);
    }

    private void OnDestroy()
    {
        if (!IsLocalPlayer) return;
        if (audioListenerController == null) return;

        audioListenerController.Unbind();
    }
}