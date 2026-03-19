using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIRoot : MonoBehaviour
{
    public static UIRoot instance;

    public GameObject pausePanel;
    public GameObject shopPanel;

    private void Awake()
    {
        if(instance == null)
            instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (GameManager.instance.isMainScene && MatchManager.instance.currentRoundState == RoundState.Preparation)
            {
                UIPrompt.instance.RemovePrompts();
                OpenPanel(shopPanel);
            }
            else
            {
                CloseShopPanel();
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.instance.isMainScene)
            {
                OpenPanel(pausePanel);
            }
            else
            {
                ClosePanel(pausePanel);
                CloseShopPanel();
            }
        }
    }

    public void OpenPanel(GameObject panel)
    {
        GameManager.instance.isMainScene = false;
        Cursor.lockState = CursorLockMode.None;
        panel.SetActive(true);
    }

    public void ClosePanel(GameObject panel)
    {
        GameManager.instance.isMainScene = true;
        Cursor.lockState = CursorLockMode.Locked;
        panel.SetActive(false);
    }

    public void CloseShopPanel()
    {
        if(MatchManager.instance.currentRoundState == RoundState.Preparation)
            UIPrompt.instance.ShowBuyPrompt();
        ClosePanel(shopPanel);
    }
}
