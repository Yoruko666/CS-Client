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
        EventCenter.Subscribe<RoundState>(GameEvents.RoundStateChanged, OnRoundStateChanged);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<RoundState>(GameEvents.RoundStateChanged, OnRoundStateChanged);
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
                // 胜负 prompt 由 MatchManager.Win/Lose 触发，这里不动
                break;
        }
    }

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

