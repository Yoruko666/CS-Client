using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

public class HallManager : SingletonMono<HallManager>
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

    private bool inHall = true;
    private bool isLoading = false;
    private volatile bool running = true;

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
                        mapSceneHandle = Addressables.LoadSceneAsync(MapAddressTable.Instance.maps[start.map], LoadSceneMode.Additive, true);

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
                PlayerReady playerReady = new(NetworkManager.instance.uid);
                NetworkManager.Send(MessageType.Ready, playerReady);
            }
        }
    }

    public void Send(HallMessage message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        socket.Send(buffer);
    }

    private void Receive()
    {
        byte[] data = new byte[1024];
        StringBuilder buffer = new();
        while (running)
        {
            try
            {
                int len = socket.Receive(data);
                if (len <= 0) break;
                buffer.Append(Encoding.UTF8.GetString(data, 0, len));

                // 处理 TCP 粘包：从 buffer 中提取所有完整 JSON 对象
                foreach (string json in ExtractJsonObjects(buffer))
                {
                    try
                    {
                        HallMessage msg = JsonConvert.DeserializeObject<HallMessage>(json);
                        if (msg != null) messageList.Enqueue(msg);
                    }
                    catch (JsonException ex)
                    {
                        Debug.LogWarning($"[HallManager] JSON 解析失败：{ex.Message}, 原文: {json}");
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // socket.Close() 触发，正常退出
                break;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted ||
                    ex.SocketErrorCode == SocketError.OperationAborted ||
                    ex.SocketErrorCode == SocketError.ConnectionReset ||
                    !running)
                    break;
                Debug.LogWarning($"[HallManager] socket error: {ex.SocketErrorCode}");
                break;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                break;
            }
        }
    }

    /// <summary>
    /// 从缓冲区中提取所有完整 JSON 对象（按 {} 深度匹配，正确处理字符串内的 {}）。
    /// 与 MainServer.Program.ExtractJsonObjects 行为一致。
    /// </summary>
    private static List<string> ExtractJsonObjects(StringBuilder buffer)
    {
        string data = buffer.ToString();
        int pos = 0;
        var results = new List<string>();

        while (pos < data.Length)
        {
            while (pos < data.Length && char.IsWhiteSpace(data[pos])) pos++;
            if (pos >= data.Length) break;
            if (data[pos] != '{') { pos++; continue; }

            int depth = 1;
            bool inString = false;
            bool escaped = false;
            int start = pos;
            pos++;

            while (pos < data.Length && depth > 0)
            {
                char c = data[pos];
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = !inString;
                else if (!inString)
                {
                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                }
                pos++;
            }

            if (depth == 0)
            {
                results.Add(data[start..pos]);
                buffer.Remove(0, pos);
                data = buffer.ToString();
                pos = 0;
            }
            else
            {
                // 半个 JSON，等下次再续
                break;
            }
        }

        return results;
    }

    public void Match()
    {
        Match match = new(NetworkConfigManager.instance.uid, gameMode);
        Send(new HallMessage(HallMessageType.Match, JsonConvert.SerializeObject(match)));
    }

    public void StartGame()
    {
        running = false;
        try { socket?.Close(); } catch { }
        inHall = false;
        isLoading = false;
        SceneManager.UnloadSceneAsync("Hall");
    }

    private void OnApplicationQuit()
    {
        running = false;
        try { socket?.Close(); } catch { }
    }
}

public enum GameMode
{
    ModePractice, Mode1v1, Mode3v3
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
    public int uid;
    public Connect(int uid)
    {
        this.uid = uid; 
    }
}

public class Match
{
    public int uid;
    public GameMode mode;
    public Match(int uid, GameMode mode)
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