using Photon.Pun;
using UnityEngine;

public class PhotonServerInfo : MonoBehaviour
{
    private static PhotonServerInfo Instance;

    [SerializeField] private int sendRate = 60;
    [SerializeField] private int serializationRate = 20;

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

        PhotonNetwork.NetworkStatisticsEnabled = true;
        PhotonNetwork.SendRate = sendRate;
        PhotonNetwork.SerializationRate = serializationRate;
    }
}
