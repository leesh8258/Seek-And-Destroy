using Photon.Pun;
using UnityEngine;

public class PhotonServerInfo : MonoBehaviour
{
    private static PhotonServerInfo Instance;

    [SerializeField] private int sendRate = 60;
    [SerializeField] private int serializationRate = 20;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.NetworkStatisticsEnabled = true;
        PhotonNetwork.SendRate = sendRate;
        PhotonNetwork.SerializationRate = serializationRate;
    }

    private void OnDestroy()
    {
        if (Instance != null)
        {
            Instance = null;
        }
    }
}
