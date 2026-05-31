using UnityEngine;

public class NetworkConfigManager : MonoBehaviour
{
    public static NetworkConfigManager instance;

    [Header("·þÎñÆ÷µØÖ·")]
    public string serverAddress = "127.0.0.1";

    [HideInInspector] public int uid;
    [HideInInspector] public int serverPort;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
}
