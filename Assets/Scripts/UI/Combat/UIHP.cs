using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIHP : MonoBehaviour
{
    private TextMeshProUGUI text;
    private PlayerState playerState;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        playerState = NetworkManager.instance.localPlayer.GetComponent<PlayerState>();
    }

    private void Update()
    {
        text.text = playerState.HP.ToString();
    }
}
