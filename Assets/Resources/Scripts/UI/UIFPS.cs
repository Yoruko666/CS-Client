using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIFPS : MonoBehaviour
{
    public TextMeshProUGUI text;

    float timer = 0;

    void Update()
    {
        timer += Time.deltaTime;
        while (timer > 0.1f)
        {
            timer -= 0.1f;
            text.text = $"FPS: {(int)(1f / Time.deltaTime)}";
        }
    }
}
