using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public static MatchManager instance;

    [HideInInspector] public bool gameStart = false;
    private readonly int ROUND_TO_WIN = 10;
    private Dictionary<RoundState, float> round_time;

    private int selfScore, oppoScore;
    private int currentRound;
     public RoundState currentRoundState;
    [HideInInspector] public float roundTimer;

    [HideInInspector] public int playerNum;
    public MapConfig mapConfig;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

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
            foreach (PlayerEntity entity in NetworkManager.instance.playerPool.Values)
            {
                entity.tp.Initialize();
            }
        }

        // UI / 音效 / 其他响应通过事件分发，订阅者各自处理
        EventCenter.Invoke(GameEvents.RoundStateChanged, progress);
    }

    public void Win()
    {
        selfScore++;
        UITeamInfo.instance.selfScore.text = selfScore.ToString();
        UIPrompt.instance.ShowWonPrompt();
    }

    public void Lose()
    {
        oppoScore++;
        UITeamInfo.instance.oppoScore.text = oppoScore.ToString();
        UIPrompt.instance.ShowLostPrompt();
    }
}


public enum RoundState
{
    Preparation, InProgress, RoundOver
}