using TMPro;
using UnityEngine;

public class UIHP : MonoBehaviour
{
    private TextMeshProUGUI text;
    private PlayerState playerState;

    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        playerState = NetworkManager.instance.LocalEntity.state;
        // 首帧立刻显示一次
        text.text = playerState.HP.ToString();
    }

    private void OnEnable()
    {
        EventCenter.Subscribe<int>(GameEvent.LocalPlayerHpChanged, OnHpChanged);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<int>(GameEvent.LocalPlayerHpChanged, OnHpChanged);
    }

    private void OnHpChanged(int newHp)
    {
        if (text != null) text.text = newHp.ToString();
    }
}

