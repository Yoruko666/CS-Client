using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIGameModes : MonoBehaviour
{
    public Transform BtnPractice;
    public Transform Btn1V1;
    public Transform Btn5V5;

    private Dictionary<GameMode, Transform> modeButton = new Dictionary<GameMode, Transform>();

    private void Start()
    {
        modeButton.Add(GameMode.ModePractice, BtnPractice);
        modeButton.Add(GameMode.Mode1v1, Btn1V1);
        modeButton.Add(GameMode.Mode5v5, Btn5V5);
        SelectMode(GameMode.ModePractice);
    }

    public void SelectMode(GameMode mode)
    {
        modeButton[HallManager.instance.gameMode].GetComponent<UIModeButton>().SelectExit();
        HallManager.instance.gameMode = mode;
        modeButton[HallManager.instance.gameMode].GetComponent<UIModeButton>().SelectEnter();
    }
}
