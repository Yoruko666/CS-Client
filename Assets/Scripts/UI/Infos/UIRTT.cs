using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIRTT : MonoBehaviour
{
    public static UIRTT instance; 

    private float pingPongTimer = 0;
    private int pingPoingTick = 0;
    private int tick = 0;

    private float pingTime = 0;
    private float pongTime = 0;
    private static PingPong pingPong;
    public bool received = false;
    public int RTT = 0;

    public TextMeshProUGUI text;

    private void Awake()
    {
        if(instance == null)
            instance = this;
        else Destroy(gameObject);

        text = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        pingPong = new PingPong(NetworkManager.instance.playerName, 0);
    }

    void Update()
    {
        pingPongTimer += Time.deltaTime;
        while (pingPongTimer >= 1f)
        {
            if (received)
            {
                RTT = (int)((pongTime - pingTime) * 1000);
                RTT = Mathf.Clamp(RTT, 0, 999);
            }
            else RTT = 999;

            pingPongTimer -= 1f;
            tick = pingPoingTick;
            pingTime = Time.time;
            pingPong.tick = pingPoingTick;
            NetworkManager.SendMessage(MessageType.PingPong, pingPong);
            pingPoingTick = (++pingPoingTick) % 64;
            received = false;

            text.text = $"{RTT} ms";
            if(RTT < 100) text.color = Color.green;
            else if(RTT < 200) text.color = Color.yellow;
            else text.color = Color.red;
        }
    }

    public void ReceivePong(int receiveTick)
    {
        if (tick == receiveTick)
        {
            pongTime = Time.time;
            received = true;
        }
    }
}
