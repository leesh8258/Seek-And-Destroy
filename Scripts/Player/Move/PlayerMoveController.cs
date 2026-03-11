using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using VInspector;

public class PlayerMoveController : MonoBehaviourPun
{
    [Header("Move / Rotate")]
    [SerializeField, ReadOnly] private float moveSpeed;
    [SerializeField] private float rotateSpeed = 720f;

    [Header("Footstep")]
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float footstepRayStartHeight = 0.3f;
    [SerializeField] private float footstepRayDistance = 1.2f;
    [SerializeField] private float footstepMinInterval = 0.05f;
    [SerializeField] private AudioSource footStepSource;

    private CharacterController characterController;
    private Transform cameraTransform;
    
    private float lastFootstepTime = -1f;
    private float gravity = -25f;
    private float gravityVelocity = 0f;

    private Vector3 worldMoveDir;
    public Vector3 WorldMoveDir => worldMoveDir;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void Initialize(int characterId)
    {
        if (!DBManager.Instance.TryGet<CharacterSO>(characterId, out CharacterSO characterSO)) return;

        moveSpeed = characterSO.moveSpeed;
    }

    public void SetCamera(Camera camera)
    {
        cameraTransform = camera.transform;
    } 

    public void Tick(Vector2 moveInput, Vector3 aimDirection)
    {
        ApplyRotation(aimDirection);
        ApplyMovement(moveInput);
    }

    private void ApplyRotation(Vector3 _aimDirection)
    {
        Vector3 aimDirection = _aimDirection;
        aimDirection.y = 0.0f;

        if (aimDirection.sqrMagnitude <= 0.000001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    private void ApplyMovement(Vector2 moveInput)
    {
        worldMoveDir = Vector3.zero;

        if (characterController == null) return;
        if (cameraTransform == null) return;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0.0f;
        camRight.y = 0.0f;

        if (camForward.sqrMagnitude > 0.000001f) camForward.Normalize();
        if (camRight.sqrMagnitude > 0.000001f) camRight.Normalize();

        Vector3 worldMove = camRight * moveInput.x + camForward * moveInput.y;
        if (worldMove.sqrMagnitude > 1f) worldMove.Normalize();

        worldMoveDir = worldMove;

        Vector3 velocity = worldMove * moveSpeed;

        if (characterController.isGrounded)
        {
            if (gravityVelocity < 0f) gravityVelocity = -2f;
        }

        else
        {
            gravityVelocity += gravity * Time.deltaTime;
        }

        velocity.y = gravityVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void OnFootStep()
    {
        if (!photonView.IsMine) return;
        if (Time.time - lastFootstepTime < footstepMinInterval) return;

        PlayFootstepSound(transform.position);

        if (photonView.IsMine)
        {
            photonView.RPC(nameof(RpcPlayFootstep), RpcTarget.Others);
        }

        lastFootstepTime = Time.time;
    }

    [PunRPC]
    private void RpcPlayFootstep(PhotonMessageInfo info)
    {
        Player owner = photonView.Owner;
        if (owner != null && info.Sender != owner) return;

        PlayFootstepSound(transform.position);
    }

    private void PlayFootstepSound(Vector3 position)
    {
        RaycastHit hit;
        Vector3 origin = position + (Vector3.up * footstepRayStartHeight);
        Ray ray = new Ray(origin, Vector3.down);

        bool isHit = Physics.Raycast(ray, out hit, footstepRayDistance, groundLayerMask, QueryTriggerInteraction.Ignore);
        if (!isHit) return;

        MapEnvironmentSoundHandler handler = hit.collider.GetComponentInParent<MapEnvironmentSoundHandler>();
        if (handler == null) return;

        SoundSO walkSound = handler.WalkSound;
        if (walkSound == null) return;
        float volumeMultiplier = handler.VolumeMultiplier;

        SoundManager.Instance.PlaySoundOneShot(footStepSource, walkSound, volumeMultiplier);
    }
}
