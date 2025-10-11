using Newtonsoft.Json;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HallManager : MonoBehaviour
{
    private Socket socket;
    private IPEndPoint pos;

    private GameMode gameMode = GameMode.Mode1v1;
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
        socket.Bind(pos);
        socket.Listen(1);
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
                    case HallMessageType.Start:
                        Start start = JsonConvert.DeserializeObject<Start>(msg.info);
                        persistentScene = SceneManager.LoadSceneAsync("PersistantScene", LoadSceneMode.Additive);
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

    public void SwitchMode(GameMode mode)
    {
        gameMode = mode; 
    }

    private void Receive()
    {

    }

    public void Match()
    {
        persistentScene = SceneManager.LoadSceneAsync("Persistent Scene", LoadSceneMode.Additive);
        mapScene = SceneManager.LoadSceneAsync(Maps.Instance.maps[0], LoadSceneMode.Additive);
        persistentScene.allowSceneActivation = false;
        mapScene.allowSceneActivation = false;
        menu.SetActive(false);
        loading.SetActive(true);
    }
}

public enum GameMode
{
    Mode1v1, Mode3v3, Mode5v5
}
public enum HallMessageType
{
    Match, Start
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

public class Match
{
    public string playerName;
    public GameMode mode;
}

public class Start
{
    public int port;
    public int map;
}