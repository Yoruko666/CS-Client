using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    public string playerName;
    private string infoPlayerName;

    private static UdpClient udpClient;
    private static IPEndPoint serverEndPoint;
    private static ConcurrentQueue<Message> messageList = new ConcurrentQueue<Message>();

    private Dictionary<string, GameObject> playerPool = new Dictionary<string, GameObject>();

    public GameObject localPlayer;
    public static NetworkManager instance;

    private int tick;
    private float tickTimer;
    private readonly static float TICK_INTERVAL = 1f / 30f;
    private readonly static int BUFFER_SIZE = 1024;
    [HideInInspector] public static int reconciliationTime;

    private PlayerInputInfo[] inputBuffer = new PlayerInputInfo[BUFFER_SIZE];
    private PlayerStateInfo[] stateBuffer = new PlayerStateInfo[BUFFER_SIZE];

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        int port = 0;
        string address = "127.0.0.1";
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-a":
                    address = args[i + 1];
                    break;
                case "-p":
                    port = int.Parse(args[i + 1]);
                    break;
            }
        }

        playerName = NetworkConfigManager.instance.uid;
        udpClient = new UdpClient(0);
        serverEndPoint = new IPEndPoint(IPAddress.Parse(address), NetworkConfigManager.instance.serverPort);
        Debug.Log(NetworkConfigManager.instance.serverPort);
        SendMessage(new Message(MessageType.Connect, playerName));

        Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
        receiveThread.Start();
    }

    private void Update()
    {
        while(messageList.TryDequeue(out Message msg))
        {
            GameObject player;
            switch (msg.type)
            {
                case MessageType.JoinRoom:

                    break;
                case MessageType.AllPlayersInfo:
                    List<PlayerStateInfo> playersInfo = JsonConvert.DeserializeObject<List<PlayerStateInfo>>(msg.info);
                    foreach(PlayerStateInfo playerState in playersInfo)
                    {
                        infoPlayerName = playerState.playerName;
                        if (infoPlayerName == playerName)
                        {
                            localPlayer.GetComponent<PlayerState>().ApplyPlayerState(playerState);
                            if (!CheckSync(stateBuffer[playerState.tick], playerState))
                            {
                                ++reconciliationTime;
                                PlayerController playerController = localPlayer.GetComponent<PlayerController>();
                                playerController.ApplyPlayerState(playerState);
                                int reconcileTick = playerState.tick + 1;
                                while (reconcileTick < tick)
                                {
                                    playerController.ProcessInput(inputBuffer[reconcileTick]);
                                    stateBuffer[reconcileTick] = playerController.currentState;
                                    reconcileTick = (++reconcileTick) % BUFFER_SIZE;
                                }
                            }
                        }
                        else
                        {
                            if (playerPool.ContainsKey(infoPlayerName))
                                player = playerPool[infoPlayerName];
                            else
                            {
                                player = Instantiate(Resources.Load<GameObject>("Prefabs/Character"));
                                playerPool[infoPlayerName] = player;
                            }
                            player.GetComponent<TPPlayerController>().ApplyPlayerState(playerState);
                        }
                    }
                    break;

                case MessageType.Fire:
                    PlayerFire playerFire = JsonConvert.DeserializeObject<PlayerFire>(msg.info);
                    infoPlayerName = playerFire.playerName;
                    if(infoPlayerName != playerName)
                        playerPool[infoPlayerName].GetComponent<TPWeaponManager>().Fire();
                    break;

                case MessageType.Reload:
                    PlayerReload playerReload = JsonConvert.DeserializeObject<PlayerReload>(msg.info);
                    infoPlayerName = playerReload.playerName;
                    if (infoPlayerName != playerName)
                        playerPool[infoPlayerName].GetComponent<TPWeaponManager>().Reload();
                    break;

                case MessageType.PurchaseWeapon:
                    PlayerPurchaseWeapon playerPurchaseWeapon = JsonConvert.DeserializeObject<PlayerPurchaseWeapon>(msg.info);
                    infoPlayerName = playerPurchaseWeapon.playerName;
                    int id = playerPurchaseWeapon.id;
                    if (playerName == infoPlayerName)
                    {
                        localPlayer.GetComponent<WeaponManager>().PurchaseWeapon(id);
                    }
                    break;

                case MessageType.AcquireWeapon:
                    PlayerAcquireWeapon playerAcquireWeapon = JsonConvert.DeserializeObject<PlayerAcquireWeapon>(msg.info);
                    infoPlayerName = playerAcquireWeapon.playerName;
                    if (infoPlayerName != playerName)
                        playerPool[infoPlayerName].GetComponent<TPWeaponManager>().AcquireWeapon(playerAcquireWeapon.id);
                    break;

                case MessageType.SwitchWeapon:
                    PlayerSwitchWeapon playerSwitchWeapon = JsonConvert.DeserializeObject<PlayerSwitchWeapon>(msg.info);
                    infoPlayerName = playerSwitchWeapon.playerName;
                    if (infoPlayerName != playerName)
                        playerPool[infoPlayerName].GetComponent<TPWeaponManager>().SwitchWeapon(playerSwitchWeapon.index);
                    break;

                case MessageType.Die:
                    PlayerDie playerDie = JsonConvert.DeserializeObject<PlayerDie>(msg.info);
                    infoPlayerName = playerDie.playerName;
                    playerPool[infoPlayerName].GetComponent<TPPlayerController>().Die();
                    break;
            }
        }
        tickTimer += Time.deltaTime;
        while(tickTimer >= TICK_INTERVAL)
        {
            tickTimer -= TICK_INTERVAL;
            HandleTick();
            tick = (++tick) % BUFFER_SIZE;
        }
    }

    private void OnApplicationQuit()
    {
        udpClient.Close();
    }

    private void HandleTick()
    {
        PlayerController playerController = localPlayer.GetComponent<PlayerController>();
        PlayerInputInfo inputInfo = playerController.GetInputInfo();
        playerController.ProcessInput(inputInfo);
        inputInfo.tick = tick;
        inputBuffer[tick] = inputInfo;
        stateBuffer[tick] = playerController.currentState;
        SendMessage(new Message(MessageType.InputInfo, JsonConvert.SerializeObject(inputInfo)));
    }

    public static void SendMessage(Message msg)
    {
        string str = JsonConvert.SerializeObject(msg);
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        udpClient.Send(bytes, bytes.Length, serverEndPoint);
    }

    public static void ReceiveMessage()
    {
        while(udpClient != null)
        {
            try
            {
                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remote);
                string str = Encoding.UTF8.GetString(data);
                Message msg = JsonConvert.DeserializeObject<Message>(str);
                messageList.Enqueue(msg);
            }
            catch (SocketException ex)
            {
                Debug.Log("Socket error");
            }
        }
    }

    private bool CheckSync(PlayerStateInfo localState, PlayerStateInfo serverState)
    {
        if (localState == null) return true;
        float positionDistance = Vector3.Distance(localState.GetPosition(), serverState.GetPosition());
        float rotationYDiff = Mathf.Abs(Mathf.DeltaAngle(localState.rotationY, serverState.rotationY));
        float rotationXDiff = Mathf.Abs(Mathf.DeltaAngle(localState.rotationX, serverState.rotationX));
        if (positionDistance > 0.2f || rotationYDiff > 1f || rotationXDiff > 1f) return false;
        return true;
    }
}