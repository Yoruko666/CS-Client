using System.Collections.Generic;
using UnityEngine;

public class MatchManager : SingletonMono<MatchManager>
{
    [HideInInspector] public bool gameStart = false;
    private readonly int ROUND_TO_WIN = 10;
    private Dictionary<RoundState, float> round_time;

    private int selfScore, oppoScore;
    private int currentRound;
    public RoundState currentRoundState;
    [HideInInspector] public float roundTimer;

    [HideInInspector] public int playerNum;
    public MapConfig mapConfig;

    protected override void OnSingletonAwake()
    {
        round_time = new();
        round_time.Add(RoundState.Preparation, 5f);
        round_time.Add(RoundState.InProgress, 180f);
        round_time.Add(RoundState.RoundOver, 5f);
    }

    void Start()
    {
        currentRound = 1;
        currentRoundState = RoundState.Preparation;
    }

    void Update()
    {
        if (!gameStart) return;
        roundTimer -= Time.deltaTime;
    }

    public void StartGame()
    {
        gameStart = true;
        SwitchProgress(RoundState.Preparation);
    }

    public void SwitchProgress(RoundState progress)
    {
        roundTimer = round_time[progress];
        currentRoundState = progress;

        // 实体相关的初始化：MatchManager 直接持有玩家引用，留在这里合理
        if (progress == RoundState.Preparation)
        {
            PlayerController.instance.Initialize();
            NetworkManager.instance.LocalEntity.weapon.Initialize();
            foreach (RemotePlayerEntity entity in NetworkManager.instance.playerPool.Values)
            {
                entity.tp.Initialize();
            }
        }

        EventCenter.Invoke(GameEvent.RoundStateChanged, progress);
    }

    public void Win()
    {
        selfScore++;
        EventCenter.Invoke(GameEvent.RoundWon, selfScore);
    }

    public void Lose()
    {
        oppoScore++;
        EventCenter.Invoke(GameEvent.RoundLost, oppoScore);
    }
}


public enum RoundState
{
    Preparation, InProgress, RoundOver
}