using UnityEngine;

public class UIPrompt : MonoBehaviour
{
    public static UIPrompt instance;

    public GameObject buyPrompt;
    public GameObject wonPrompt;
    public GameObject lostPrompt;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        EventCenter.Subscribe<RoundState>(GameEvent.RoundStateChanged, OnRoundStateChanged);
        EventCenter.Subscribe<int>(GameEvent.RoundWon,  OnWon);
        EventCenter.Subscribe<int>(GameEvent.RoundLost, OnLost);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<RoundState>(GameEvent.RoundStateChanged, OnRoundStateChanged);
        EventCenter.Unsubscribe<int>(GameEvent.RoundWon,  OnWon);
        EventCenter.Unsubscribe<int>(GameEvent.RoundLost, OnLost);
    }

    private void OnRoundStateChanged(RoundState s)
    {
        switch (s)
        {
            case RoundState.Preparation:
                RemovePrompts();
                ShowBuyPrompt();
                break;
            case RoundState.InProgress:
                RemovePrompts();
                break;
            case RoundState.RoundOver:
                // 胜负 prompt 由 RoundWon / RoundLost 事件触发
                break;
        }
    }

    private void OnWon(int _)  => ShowWonPrompt();
    private void OnLost(int _) => ShowLostPrompt();

    public void ShowBuyPrompt() => buyPrompt.SetActive(true);
    public void ShowWonPrompt() => wonPrompt.SetActive(true);
    public void ShowLostPrompt() => lostPrompt.SetActive(true);

    public void RemovePrompts()
    {
        buyPrompt.SetActive(false);
        wonPrompt.SetActive(false);
        lostPrompt.SetActive(false);
    }
}

