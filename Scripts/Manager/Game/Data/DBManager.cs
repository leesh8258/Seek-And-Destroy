using UnityEngine;
using VInspector;

public class DBManager : MonoBehaviour
{
    public static DBManager Instance;

    [Header("Weapon DB")]
    [SerializeField] private SerializedDictionary<int, WeaponSO> weaponDB;

    [Header("Character DB")]
    [SerializeField] private SerializedDictionary<int, CharacterSO> characterDB;

    [Header("Map DB")]
    [SerializeField] private SerializedDictionary<int, MapSO> mapDB;

    [Header("Sound Setting DB")]
    [SerializeField] private SerializedDictionary<int, SoundSourceSettingSO> soundSourceSettingDB;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public bool TryGet<T>(int key, out T value) where T : ScriptableObject
    {
        value = null;

        if (typeof(T) == typeof(WeaponSO))
        {
            if (GetFrom(weaponDB, key, out WeaponSO weaponSO))
            {
                value = weaponSO as T;
                return true;
            }

            return false;
        }

        else if (typeof(T) == typeof(CharacterSO))
        {
            if (GetFrom(characterDB, key, out CharacterSO characterSO))
            {
                value = characterSO as T;
                return true;
            }

            return false;
        }

        else if (typeof(T) == typeof(MapSO))
        {
            if (GetFrom(mapDB, key, out MapSO mapSO))
            {
                value = mapSO as T;
                return true;
            }

            return false;
        }

        else if (typeof(T) == typeof(SoundSourceSettingSO))
        {
            if (GetFrom(soundSourceSettingDB, key, out SoundSourceSettingSO soundSourceSettingSO))
            {
                value = soundSourceSettingSO as T;
                return true;
            }

            return false;
        }

        return false;
    }
    public int GetCount<T>() where T : ScriptableObject
    {
        if (typeof(T) == typeof(WeaponSO))
        {
            return weaponDB != null ? weaponDB.Count : 0;
        }
        
        else if (typeof(T) == typeof(CharacterSO))
        {
            return characterDB != null ? characterDB.Count : 0;
        }
        
        else if (typeof(T) == typeof(MapSO))
        {
            return mapDB != null ? mapDB.Count : 0;
        }
        
        else if (typeof(T) == typeof(SoundSourceSettingSO))
        {
            return soundSourceSettingDB != null ? soundSourceSettingDB.Count : 0;
        }

        return 0;
    }

    private bool GetFrom<T>(SerializedDictionary<int, T> db, int key, out T value) where T : ScriptableObject
    {
        if (db != null && db.TryGetValue(key, out value) && value != null)
        {
            return true;
        }

        value = null;
        return false;
    }
}
