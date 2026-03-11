using UnityEngine;
using UnityEngine.UI;

public class SelectButtonBuilder : MonoBehaviour
{
    private enum Type
    {
        Character,
        Weapon,
        Map
    }

    [Header("Select Type")]
    [SerializeField] private Type type;

    [Header("Button Prefab")]
    [SerializeField] private GameObject buttonPrefab;

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject buttonRoot;

    private int currentTypeCount;

    private void Start()
    {
        CloseUI();

        if (type == Type.Character)
        {
            currentTypeCount = DBManager.Instance.GetCount<CharacterSO>();
            BuildCharacterButtons();
        }

        else if (type == Type.Weapon)
        {
            currentTypeCount = DBManager.Instance.GetCount<WeaponSO>();
            BuildWeaponButtons();
        }

        else if (type == Type.Map)
        {
            currentTypeCount = DBManager.Instance.GetCount<MapSO>();
            BuildMapButtons();
        }
    }

    private void BuildCharacterButtons()
    {
        for (int i = 0; i < currentTypeCount; i++)
        {
            if (DBManager.Instance.TryGet<CharacterSO>(i, out CharacterSO characterSO))
            {
                int index = i;

                GameObject buttonInstance = Instantiate(buttonPrefab, buttonRoot.transform);
                
                Button button = buttonInstance.GetComponentInChildren<Button>();
                button.onClick.AddListener(() => ApplyLocalCharacter(index));
                button.onClick.AddListener(CloseUI);

                Image buttonImage = button.GetComponent<Image>();
                buttonImage.sprite = characterSO.characterSprite;
            }
        }
    }

    private void BuildWeaponButtons()
    {
        for (int i = 0; i < currentTypeCount ; i++)
        {
            if (DBManager.Instance.TryGet<WeaponSO>(i, out WeaponSO weaponSO))
            {
                int index = i;
                GameObject buttonInstance = Instantiate(buttonPrefab, buttonRoot.transform);
                
                Button button = buttonInstance.GetComponentInChildren<Button>();
                button.onClick.AddListener(() => ApplyLocalWeapon(index));
                button.onClick.AddListener(CloseUI);
                
                Image buttonImage = button.GetComponent<Image>();
                buttonImage.sprite = weaponSO.weaponSprite;
            }
        }
    }

    private void BuildMapButtons()
    {
        for (int i = 0; i < currentTypeCount ; i++)
        {
            if (DBManager.Instance.TryGet<MapSO>(i, out MapSO mapSO))
            {
                int index = i;
                GameObject buttonInstance = Instantiate(buttonPrefab, buttonRoot.transform);
                
                Button button = buttonInstance.GetComponentInChildren<Button>();
                button.onClick.AddListener(() => ApplyHostMap(index));
                button.onClick.AddListener(CloseUI);
                
                Image buttonImage = button.GetComponent<Image>();
                buttonImage.sprite = mapSO.mapSprite;
            }
        }
    }

    private void ApplyLocalWeapon(int weaponId)
    {
        if (RoomNetworkManager.Instance == null) return;
        RoomNetworkManager.Instance.SetLocalWeapon(weaponId);
    }

    private void ApplyLocalCharacter(int characterId)
    {
        if (RoomNetworkManager.Instance == null) return;
        RoomNetworkManager.Instance.SetLocalCharacter(characterId);
    }

    private void ApplyHostMap(int mapId)
    {
        if (RoomNetworkManager.Instance == null) return;
        if (!RoomNetworkManager.Instance.IsMasterClient) return;

        RoomNetworkManager.Instance.SetRoomMap(mapId);
    }

    private void CloseUI()
    {
        panelRoot.SetActive(false);
    }
}
