using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIRoot : MonoBehaviour
{
    public GameObject pausePanel;
    public GameObject shopPanel;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (GameManager.instance.isMainScene)
            {
                OpenPanel(shopPanel);
            }
            else
            {
                ClosePanel(shopPanel);
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
                ClosePanel(shopPanel);
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
}
