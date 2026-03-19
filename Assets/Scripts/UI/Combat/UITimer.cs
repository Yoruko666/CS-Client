using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITimer : MonoBehaviour
{
    private TextMeshProUGUI text;

    void Start()
    {
        text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        int timer = (int)MatchManager.instance.roundTimer;
        text.text = $"{timer / 60}:{timer % 60}";
    }
}
