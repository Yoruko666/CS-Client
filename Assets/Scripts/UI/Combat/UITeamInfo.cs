using TMPro;
using UnityEngine;

public class UITeamInfo : MonoBehaviour
{
    public static UITeamInfo instance;

    public TextMeshProUGUI selfScore;
    public TextMeshProUGUI oppoScore;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        EventCenter.Subscribe<int>(GameEvents.RoundWon, OnWon);
        EventCenter.Subscribe<int>(GameEvents.RoundLost, OnLost);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<int>(GameEvents.RoundWon, OnWon);
        EventCenter.Unsubscribe<int>(GameEvents.RoundLost, OnLost);
    }

    private void OnWon(int score)  => selfScore.text = score.ToString();
    private void OnLost(int score) => oppoScore.text = score.ToString();
}
