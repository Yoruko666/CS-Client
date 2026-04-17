using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HallManager : MonoBehaviour
{
    private Socket socket;
    private IPEndPoint pos;

    [HideInInspector] public GameMode gameMode = GameMode.ModePractice;
    private ConcurrentQueue<HallMessage> messageList = new ConcurrentQueue<HallMessage>();

    public GameObject menu;
    public GameObject loading;
    public Image map;
    public Image slider;

    private AsyncOperationHandle<SceneInstance> persistentSceneHandle;
    private AsyncOperationHandle<SceneInstance> mapSceneHandle;

    public static HallManager instance; 

    private bool inHall = true;
    private bool isLoading = false;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-a":
                    NetworkConfigManager.instance.serverAddress = args[i + 1];
                    break;
            }
        }

        pos = new IPEndPoint(IPAddress.Parse(NetworkConfigManager.instance.serverAddress), 25000);
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(pos);
        Thread receiveThread = new(new ThreadStart(Receive));
        receiveThread.Start();
    }

    void Update()
    {
        if (!inHall) return;
        if (!isLoading)
        {
            while (messageList.TryDequeue(out HallMessage msg))
            {
                switch (msg.type)
                {
                    case HallMessageType.Connect:
                        Connect connect = JsonConvert.DeserializeObject<Connect>(msg.info);
                        NetworkConfigManager.instance.uid = connect.uid;
                        break;

                    case HallMessageType.Start:
                        Start start = JsonConvert.DeserializeObject<Start>(msg.info);
                        NetworkConfigManager.instance.serverPort = start.port;

                        persistentSceneHandle = Addressables.LoadSceneAsync("Persistent Scene", LoadSceneMode.Additive, true);
                        mapSceneHandle = Addressables.LoadSceneAsync(Maps.Instance.maps[start.map], LoadSceneMode.Additive, true);

                        menu.SetActive(false);
                        loading.SetActive(true);

                        isLoading = true;
                        break;
                }
            }
        }
        else
        {
            slider.fillAmount = (persistentSceneHandle.PercentComplete + mapSceneHandle.PercentComplete) / 2;
            if (slider.fillAmount >= 1f && persistentSceneHandle.IsDone && mapSceneHandle.IsDone)
            {
                PlayerReady playerReady = new(NetworkManager.instance.playerName);
                NetworkManager.SendMessage(MessageType.Ready, playerReady);
            }
        }
    }

    public void SendMessage(HallMessage message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        socket.Send(buffer);
    }

    private void Receive()
    {
        byte[] data = new byte[1024];
        while (true)
        {
            int len = socket.Receive(data);
            string str = Encoding.UTF8.GetString(data, 0, len);
            HallMessage msg = JsonConvert.DeserializeObject<HallMessage>(str);
            messageList.Enqueue(msg);
        }
    }

    public void Match()
    {
        Match match = new(NetworkConfigManager.instance.uid, gameMode);
        SendMessage(new HallMessage(HallMessageType.Match, JsonConvert.SerializeObject(match)));
    }

    public void StartGame()
    {
        socket.Close();
        inHall = false;
        isLoading = false;
        SceneManager.UnloadSceneAsync("Hall");
    }

    private void OnApplicationQuit()
    {
        socket.Close();
    }
}

public enum GameMode
{
    ModePractice, Mode1v1, Mode5v5
}

public enum HallMessageType
{
    Connect, Match, Start
}

public class HallMessage
{
    public HallMessageType type;
    public string info;
    public HallMessage(HallMessageType type, string msg)
    {
        this.type = type;
        this.info = msg;
    }
}

public class Connect
{
    public string uid;
    public Connect(string uid)
    {
        this.uid = uid; 
    }
}

public class Match
{
    public string uid;
    public GameMode mode;
    public Match(string uid, GameMode mode)
    {
        this.uid = uid;
        this.mode = mode;
    }
}

public class Start
{
    public int port;
    public int map;
}