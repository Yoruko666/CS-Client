using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkConfigManager : MonoBehaviour
{
    public static NetworkConfigManager instance;

    public string uid;
    public string serverAddress = "127.0.0.1";
    public int serverPort;

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
