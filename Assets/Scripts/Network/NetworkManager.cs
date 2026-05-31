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
    public int slot, team;
    public int uid;

    private static UdpClient udpClient;
    private static IPEndPoint serverEndPoint;
    private static ConcurrentQueue<(MessageType, string)> messageList = new();

    public Dictionary<int, PlayerEntity> playerPool = new();

    public GameObject localPlayer;

    private PlayerEntity _localEntity;

    public PlayerEntity LocalEntity
    {
        get
        {
            if (_localEntity == null && localPlayer != null)
                _localEntity = PlayerEntity.CreateLocal(localPlayer, uid, slot, team);
            return _localEntity;
        }
    }

    public static NetworkManager instance;

    private uint tick;
    private float tickTimer;
    public readonly static float TICK_INTERVAL = 1f / 128f;
    private readonly static int BUFFER_SIZE = 1024;
    [HideInInspector] public static int reconciliationTime;

    private PlayerInputInfo[] inputBuffer = new PlayerInputInfo[BUFFER_SIZE];
    private PlayerStateInfo[] stateBuffer = new PlayerStateInfo[BUFFER_SIZE];

    private Dictionary<MessageType, Action<string>> handlers;

    private GameObject _enemyPrefab;

    private static int IndexOf(uint t) => (int)(t % (uint)BUFFER_SIZE);

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

        uid = NetworkConfigManager.instance.uid;
        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        handlers = new Dictionary<MessageType, Action<string>>
        {
            { MessageType.Start,           OnStart           },
            { MessageType.GameProgress,    OnGameProgress    },
            { MessageType.AllPlayersInfo,  OnAllPlayersInfo  },
            { MessageType.Fire,            OnFire            },
            { MessageType.Reload,          OnReload          },
            { MessageType.AcquireWeapon,   OnAcquireWeapon   },
            { MessageType.SwitchWeapon,    OnSwitchWeapon    },
            { MessageType.Kill,            OnKill            },
            { MessageType.Hit,             OnHit             },
            { MessageType.RoundEnd,        OnRoundEnd        },
            { MessageType.PingPong,        OnPingPong        },
            { MessageType.Chat,            OnChat            }
        };
    }

    private void Start()
    {
        string[] args = Environment.GetCommandLineArgs();

        udpClient = new UdpClient(0);
        serverEndPoint = new IPEndPoint(IPAddress.Parse(NetworkConfigManager.instance.serverAddress), NetworkConfigManager.instance.serverPort);

        Send(MessageType.Connect, new PlayerConnect(uid));

        Thread receiveThread = new(new ThreadStart(ReceiveMessage));
        receiveThread.Start();
    }

    private void Update()
    {
        while (messageList.TryDequeue(out var data))
        {
            if (handlers.TryGetValue(data.Item1, out var handler))
            {
                try { handler(data.Item2); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
            else
            {
                Debug.LogWarning($"No handler for message type: {data.Item1}");
            }
        }

        tickTimer += Time.deltaTime;
        while (tickTimer >= TICK_INTERVAL)
        {
            tickTimer -= TICK_INTERVAL;
            HandleTick();
            tick++;
        }
    }

    // ============ Message Handlers ============

    private void OnStart(string msg)
    {
        HallManager.instance.StartGame();
        MatchManager.instance.StartGame();

        var playersInfo = JsonConvert.DeserializeObject<List<PlayerStateInfo>>(msg);
        MatchManager.instance.playerNum = playersInfo.Count;

        // 一次性预加载敌人 prefab。
        // 同步加载，确保 playerPool 在收到 AllPlayersInfo / Fire 等消息前已就绪。
        if (_enemyPrefab == null)
            _enemyPrefab = Addressables.LoadAssetAsync<GameObject>("Enemy").WaitForCompletion();

        for (int i = 0; i < playersInfo.Count; i++)
        {
            PlayerStateInfo playerState = playersInfo[i];
            if (playerState.uid == uid)
            {
                slot = playerState.slot;
                team = playerState.team;
                _ = LocalEntity;     // 触发 LocalEntity 懒加载
                PlayerController.instance.Initialize();
            }
            else
            {
                GameObject enemy = Instantiate(_enemyPrefab);
                var entity = PlayerEntity.CreateRemote(enemy, playerState.uid, playerState.slot, playerState.team);
                playerPool[playerState.uid] = entity;
                entity.tp.Initialize(playerState.uid, playerState.slot);
            }
        }
    }

    private void OnGameProgress(string msg)
    {
        var gameProgress = JsonConvert.DeserializeObject<GameProgress>(msg);
        MatchManager.instance.SwitchProgress(gameProgress.progress);
    }

    private void OnAllPlayersInfo(string msg)
    {
        var playersInfo = JsonConvert.DeserializeObject<List<PlayerStateInfo>>(msg);
        foreach (PlayerStateInfo playerState in playersInfo)
        {
            int psUid = playerState.uid;
            if (psUid == uid)
            {
                ApplyLocalPlayerState(playerState);
            }
            else if (playerPool.TryGetValue(psUid, out var entity) && entity?.tp != null)
            {
                entity.tp.EnqueueSnapshot(playerState);
            }
        }
    }

    /// <summary>本地玩家状态校正 + 必要时回滚重模拟</summary>
    private void ApplyLocalPlayerState(PlayerStateInfo playerState)
    {
        var local = LocalEntity;
        local.state.ApplyPlayerState(playerState);
        local.weapon.ApplyPlayerState(playerState);

        // 服务端回传的 tick 是已经 mod BUFFER_SIZE 的值
        int serverIdx = ((playerState.tick % BUFFER_SIZE) + BUFFER_SIZE) % BUFFER_SIZE;
        if (CheckSync(stateBuffer[serverIdx], playerState)) return;

        ++reconciliationTime;
        local.fp.ApplyPlayerState(playerState);

        // 用 uint 减法的回绕特性计算需要重新模拟的 tick 段
        uint serverTick = RecoverLogicTick(playerState.tick);
        uint catchup = tick - serverTick - 1;
        if (catchup > BUFFER_SIZE) catchup = 0;        // 防御：差距过大说明数据异常
        for (uint i = 0; i < catchup; i++)
        {
            uint t = serverTick + 1 + i;
            int idx = IndexOf(t);
            if (inputBuffer[idx] == null) continue;
            local.fp.ProcessInput(inputBuffer[idx]);
            stateBuffer[idx] = local.fp.currentState;
        }
    }

    private void OnFire(string msg)
    {
        var playerFire = JsonConvert.DeserializeObject<PlayerFire>(msg);
        if (playerFire.uid != uid
            && playerPool.TryGetValue(playerFire.uid, out var entity) && entity?.tpWeapon != null)
            entity.tpWeapon.Fire(playerFire.GetHitPoint());
    }

    private void OnReload(string msg)
    {
        var playerReload = JsonConvert.DeserializeObject<PlayerReload>(msg);
        if (playerReload.uid != uid
            && playerPool.TryGetValue(playerReload.uid, out var entity) && entity?.tpWeapon != null)
            entity.tpWeapon.Reload();
    }

    private void OnAcquireWeapon(string msg)
    {
        var playerAcquireWeapon = JsonConvert.DeserializeObject<PlayerAcquireWeapon>(msg);
        if (playerAcquireWeapon.uid == uid)
        {
            LocalEntity.weapon.PurchaseWeapon(playerAcquireWeapon.id);
        }
        else if (playerPool.TryGetValue(playerAcquireWeapon.uid, out var entity) && entity?.tpWeapon != null)
        {
            entity.tpWeapon.AcquireWeapon(playerAcquireWeapon.id);
        }
    }

    private void OnSwitchWeapon(string msg)
    {
        var playerSwitchWeapon = JsonConvert.DeserializeObject<PlayerSwitchWeapon>(msg);
        if (playerSwitchWeapon.uid != uid
            && playerPool.TryGetValue(playerSwitchWeapon.uid, out var entity) && entity?.tpWeapon != null)
            entity.tpWeapon.SwitchWeapon(playerSwitchWeapon.index);
    }

    private void OnKill(string msg)
    {
        var playerKill = JsonConvert.DeserializeObject<PlayerKill>(msg);

        if (uid == playerKill.killerUid
            && playerPool.TryGetValue(playerKill.victimUid, out var dieEntity)
            && dieEntity?.tp != null)
        {
            dieEntity.tp.Die();
        }
        if (uid == playerKill.victimUid)
        {
            LocalEntity.fp.Die();
        }

        EventCenter.Invoke(GameEvent.PlayerKilled, playerKill);
    }

    private void OnHit(string msg)
    {
        var hit = JsonConvert.DeserializeObject<Hit>(msg);
        EventCenter.Invoke(GameEvent.LocalPlayerHit, hit);
    }

    private void OnRoundEnd(string msg)
    {
        var roundEnd = JsonConvert.DeserializeObject<RoundEnd>(msg);
        if (roundEnd.winTeam == team) MatchManager.instance.Win();
        else MatchManager.instance.Lose();
    }

    private void OnPingPong(string msg)
    {
        var pingPong = JsonConvert.DeserializeObject<PingPong>(msg);
        UIRTT.instance.ReceivePong(pingPong.tick);
    }

    private void OnChat(string msg)
    {
        var chat = JsonConvert.DeserializeObject<Chat>(msg);
        EventCenter.Invoke(GameEvent.Chat, chat);
    }

    private uint RecoverLogicTick(int moddedTick)
    {
        uint cur = tick;
        uint curMod = cur % (uint)BUFFER_SIZE;
        // 在 mod 域内向后倒退多少步可以到达 moddedTick
        uint diff = (curMod - (uint)moddedTick) % (uint)BUFFER_SIZE;
        return cur - diff;     // uint 减法天然回绕
    }

    private void OnApplicationQuit()
    {
        udpClient.Close();
    }

    private void HandleTick()
    {
        var local = LocalEntity;
        if (local == null || local.fp.isDie) return;

        PlayerInputInfo inputInfo = local.fp.GetInputInfo();
        local.fp.ProcessInput(inputInfo);
        local.weapon.HandleTick();

        PlayerStateInfo state = new();
        local.fp.UpdatePlayerState(ref state);
        local.weapon.UpdatePlayerState(ref state);

        inputInfo.tick = (int)(tick % (uint)BUFFER_SIZE);
        int slot = IndexOf(tick);
        inputBuffer[slot] = inputInfo;
        stateBuffer[slot] = state;
        Send(MessageType.InputInfo, inputInfo);
    }

    public static void Send<T>(MessageType type, T data)
    {
        try
        {
            byte[] typeBytes = BitConverter.GetBytes((int)type);
            string dataStr = JsonConvert.SerializeObject(data);
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataStr);
            byte[] lengthBytes = BitConverter.GetBytes(4 + dataBytes.Length);
            byte[] sendBuffer = new byte[8 + dataBytes.Length];
            Buffer.BlockCopy(lengthBytes, 0, sendBuffer, 0, 4);
            Buffer.BlockCopy(typeBytes, 0, sendBuffer, 4, 4);
            Buffer.BlockCopy(dataBytes, 0, sendBuffer, 8, dataBytes.Length);
            udpClient.Send(sendBuffer, sendBuffer.Length, serverEndPoint);
        }
        catch (JsonSerializationException ex)
        {
            Debug.Log($"JSON serialize error：{ex.Message}");
        }
        catch (SocketException ex)
        {
            Debug.Log($"UDP send error：{ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.Log($"Message send error：{ex.Message}");
        }
    }

    public static void ReceiveMessage()
    {
        while (udpClient != null)
        {
            try
            {
                IPEndPoint remote = new(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remote);

                if (data.Length < 8)
                {
                    Debug.Log("Data is too short.");
                    continue;
                }

                int length = BitConverter.ToInt32(data, 0);
                if(length != data.Length - 4)
                {
                    Debug.Log("Data length mismatch.");
                    continue;
                }

                MessageType type = (MessageType)BitConverter.ToInt32(data, 4);

                string str = Encoding.UTF8.GetString(data, 8, data.Length - 8);
                messageList.Enqueue((type, str));
            }
            catch (ObjectDisposedException)
            {
                // 应用退出时 udpClient.Close() 会触发，正常情况
                break;
            }
            catch (SocketException ex)
            {
                // 应用退出时也可能抛 Interrupted，平静退出
                if (ex.SocketErrorCode == SocketError.Interrupted ||
                    ex.SocketErrorCode == SocketError.OperationAborted)
                    break;
                Debug.LogWarning($"recv socket error: {ex.SocketErrorCode}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
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