using UnityEngine;

public class NetworkConfigManager : SingletonMono<NetworkConfigManager>
{
    [Header("服务器地址")]
    public string serverAddress = "127.0.0.1";

    [HideInInspector] public int uid;
    [HideInInspector] public int serverPort;
}
