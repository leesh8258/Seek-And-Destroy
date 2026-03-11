using UnityEngine;
using UnityEngine.UI;

public class PlayerSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject occupiedRoot;
    [SerializeField] private GameObject emptyRoot;

    [SerializeField] private Image characterImage;
    [SerializeField] private Image weaponImage;

    [SerializeField] private GameObject readyBadge;
    [SerializeField] private GameObject localBadge;

    private bool hasMember = false;
    private int isCharacterId = int.MinValue;
    private int isWeaponId = int.MinValue;
    private bool isReady = false;
    private bool isLocal = false;

    public void TickEmpty()
    {
        if (hasMember)
        {
            hasMember = false;
            if (occupiedRoot != null) occupiedRoot.SetActive(false);
            if (emptyRoot != null) emptyRoot.SetActive(true);
        }
    }

    public void TickOccupied(InfoService.RoomMemberInfo member)
    {
        if (!hasMember)
        {
            hasMember = true;
            if (occupiedRoot != null) occupiedRoot.SetActive(true);
            if (emptyRoot != null) emptyRoot.SetActive(false);

            isCharacterId = int.MinValue;
            isWeaponId = int.MinValue;
            isReady = !member.IsReady;
            isLocal = !member.IsLocal;
        }

        if (isCharacterId != member.CharacterId)
        {
            isCharacterId = member.CharacterId;
            if (characterImage != null)
            {
                if (DBManager.Instance != null && DBManager.Instance.TryGet<CharacterSO>(isCharacterId, out var so))
                {
                    characterImage.sprite = so.characterSprite;

                }

                else
                {
                    characterImage.sprite = null;
                }
            }
        }

        if (isWeaponId != member.WeaponId)
        {
            isWeaponId = member.WeaponId;
            if (weaponImage != null)
            {
                if (DBManager.Instance != null && DBManager.Instance.TryGet<WeaponSO>(isWeaponId, out var so))
                {
                    weaponImage.sprite = so.weaponSprite;
                }

                else
                {
                    weaponImage.sprite = null;
                }
            }
        }

        if (isReady != member.IsReady)
        {
            isReady = member.IsReady;
            if (readyBadge != null) readyBadge.SetActive(isReady);
        }

        if (isLocal != member.IsLocal)
        {
            isLocal = member.IsLocal;
            if (localBadge != null) localBadge.SetActive(isLocal);
        }
    }


}
