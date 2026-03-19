using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

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

        switch (progress)
        {
            case RoundState.Preparation:
                PlayerController.instance.Initialize();
                NetworkManager.instance.localPlayer.GetComponent<WeaponManager>().Initialize();
                foreach (GameObject player in NetworkManager.instance.playerPool.Values)
                {
                    player.transform.GetComponent<TPPlayerController>().Initialize();
                }
                UIPrompt.instance.RemovePrompts();
                UIPrompt.instance.ShowBuyPrompt();
                break;
            case RoundState.InProgress:
                UIRoot.instance.CloseShopPanel();
                UIPrompt.instance.RemovePrompts();
                break;
            case RoundState.RoundOver:
                break;
        }
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