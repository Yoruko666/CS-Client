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

public class NetworkManager : MonoBehaviour
{
    public int id, team;
    public string playerName;
    private string infoPlayerName;

    private static UdpClient udpClient;
    private static IPEndPoint serverEndPoint;
    private static ConcurrentQueue<Message> messageList = new();

    public Dictionary<string, GameObject> playerPool = new();

    public GameObject localPlayer;
    public static NetworkManager instance;

    private int tick;
    private float tickTimer;
    public readonly static float TICK_INTERVAL = 1f / 128f;
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

        playerName = NetworkConfigManager.instance.uid;
    }

    private void Start()
    {
        string[] args = Environment.GetCommandLineArgs();

        udpClient = new UdpClient(0);
        serverEndPoint = new IPEndPoint(IPAddress.Parse(NetworkConfigManager.instance.serverAddress), NetworkConfigManager.instance.serverPort);

        Message message = new(MessageType.Connect, playerName);
        SendMessage(message);

        Thread receiveThread = new(new ThreadStart(ReceiveMessage));
        receiveThread.Start();
    }

    private void Update()
    {
        while(messageList.TryDequeue(out Message msg))
        {
            GameObject player;
            switch (msg.type)
            {
                case MessageType.Start:
                    HallManager.instance.StartGame();
                    MatchManager.instance.StartGame();

                    List<PlayerStateInfo> playersInfo = JsonConvert.DeserializeObject<List<PlayerStateInfo>>(msg.info);
                    MatchManager.instance.playerNum = playersInfo.Count;
                    for(int i = 0; i < playersInfo.Count; i++)
                    {
                        PlayerStateInfo playerState = playersInfo[i];
                        if (playerState.playerName.Equals(playerName))
                        {
                            id = playerState.id;
                            team = playerState.team;
                            PlayerController.instance.Initialize();
                        }
                        else
                        {
                            Addressables.LoadAssetAsync<GameObject>("Enemy").Completed += (obj) =>
                            {
                                player = Instantiate(obj.Result);
                                playerPool.Add(playerState.playerName, player);
                                player.GetComponent<TPPlayerController>().Initialize(playerState.playerName, playerState.id);
                            };
                        }
                    }
                    break;

                case MessageType.GameProgress:
                    GameProgress gameProgress = JsonConvert.DeserializeObject<GameProgress>(msg.info);
                    MatchManager.instance.SwitchProgress(gameProgress.progress);
                    break;

                case MessageType.AllPlayersInfo:
                    playersInfo = JsonConvert.DeserializeObject<List<PlayerStateInfo>>(msg.info);
                    foreach (PlayerStateInfo playerState in playersInfo)
                    {
                        infoPlayerName = playerState.playerName;
                        if (infoPlayerName == playerName)
                        {
                            localPlayer.GetComponent<PlayerState>().ApplyPlayerState(playerState);
                            localPlayer.GetComponent<WeaponManager>().ApplyPlayerState(playerState);
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
                            if (playerPool.ContainsKey(infoPlayerName) && playerPool[infoPlayerName] != null)
                            {
                                player = playerPool[infoPlayerName];
                                player.GetComponent<TPPlayerController>().ApplyPlayerState(playerState);
                            }
                        }
                    }
                    break;

                case MessageType.Fire:
                    PlayerFire playerFire = JsonConvert.DeserializeObject<PlayerFire>(msg.info);
                    infoPlayerName = playerFire.playerName;
                    if (infoPlayerName != playerName)
                        playerPool[infoPlayerName].GetComponent<TPWeaponManager>().Fire(playerFire.GetHitPoint());
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
                    int weaponIid = playerPurchaseWeapon.id;
                    if (playerName == infoPlayerName)
                    {
                        localPlayer.GetComponent<WeaponManager>().PurchaseWeapon(weaponIid);
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

                case MessageType.Kill:
                    PlayerKill playerKill = JsonConvert.DeserializeObject<PlayerKill>(msg.info);
                    if(playerName == playerKill.playerKillName)
                    {
                        playerPool[playerKill.playerDieName].GetComponent<TPPlayerController>().Die();
                        AudioClip killAudio = Addressables.LoadAssetAsync<AudioClip>("Kill1").WaitForCompletion();
                        AudioManager.instance.PlayAudio(killAudio);
                        UIKillBanner.instance.ShowKillBanner(playerKill.shotHead);
                    }
                    if(playerName == playerKill.playerDieName)
                    {
                        localPlayer.GetComponent<PlayerController>().Die();
                    }
                    UIKillInfos.instance.AddKillInofo(playerKill.playerKillName, playerKill.playerDieName, playerKill.weaponId, playerKill.shotHead);
                    break;

                case MessageType.Hit:
                    Hit hit = JsonConvert.DeserializeObject<Hit>(msg.info);
                    Transform indicators = GameObject.Find("Canvas/Indicator").transform;
                    GameObject hitIndicator = Instantiate(Resources.Load<GameObject>("Prefabs/UI/HitIndicator"), indicators);
                    hitIndicator.GetComponent<UIHitIndicator>().Initialize(hit.GetPosition());
                    break;

                case MessageType.RoundEnd:
                    RoundEnd roundEnd = JsonConvert.DeserializeObject<RoundEnd>(msg.info);
                    if (roundEnd.winTeam == team)
                        MatchManager.instance.Win();
                    else MatchManager.instance.Lose();
                    break;

                case MessageType.PingPong:
                    PingPong pingPong = JsonConvert.DeserializeObject<PingPong>(msg.info);
                    UIRTT.instance.ReceivePong(pingPong.tick);
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
        if (playerController.isDie) return;

        PlayerInputInfo inputInfo = playerController.GetInputInfo();
        playerController.ProcessInput(inputInfo);

        WeaponManager weaponManager = localPlayer.GetComponent<WeaponManager>();
        weaponManager.HandleTick();

        PlayerStateInfo state = new();
        playerController.UpdatePlayerState(ref state);
        weaponManager.UpdatePlayerState(ref state);

        inputInfo.tick = tick;
        inputBuffer[tick] = inputInfo;
        stateBuffer[tick] = state;
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
                IPEndPoint remote = new(IPAddress.Any, 0);
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