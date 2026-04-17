using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkConfigManager : MonoBehaviour
{
    public static NetworkConfigManager instance;

    [HideInInspector] public string uid;
    [HideInInspector] public string serverAddress;
    [HideInInspector] public int serverPort;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

        serverAddress = "127.0.0.1";
    }
}
