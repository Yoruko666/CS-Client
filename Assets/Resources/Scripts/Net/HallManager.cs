using Newtonsoft.Json;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
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

    private AsyncOperation persistentScene;
    private AsyncOperation mapScene;

    public static HallManager instance; 
    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        pos = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25000);
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(pos);
        Thread receiveThread = new Thread(new ThreadStart(Receive));
        receiveThread.Start();
    }

    void Update()
    {
        if (menu.activeSelf)
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
                        persistentScene = SceneManager.LoadSceneAsync("Persistent Scene", LoadSceneMode.Additive);
                        mapScene = SceneManager.LoadSceneAsync(Maps.Instance.maps[start.map], LoadSceneMode.Additive);
                        persistentScene.allowSceneActivation = false;
                        mapScene.allowSceneActivation = false;
                        menu.SetActive(false);
                        loading.SetActive(true);
                        break;
                }
            }
        }
        else
        {
            slider.fillAmount = (persistentScene.progress + mapScene.progress) / 2;
            if(slider.fillAmount >= 0.9f)
            {
                persistentScene.allowSceneActivation = true;
                mapScene.allowSceneActivation = true;
                SceneManager.UnloadSceneAsync("Hall");
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
        Match match = new Match(NetworkConfigManager.instance.uid, gameMode);
        Debug.Log(JsonConvert.SerializeObject(match));
        SendMessage(new HallMessage(HallMessageType.Match, JsonConvert.SerializeObject(match)));
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