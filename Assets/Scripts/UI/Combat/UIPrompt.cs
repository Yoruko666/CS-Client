using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class UIPrompt : MonoBehaviour
{
    public static UIPrompt instance;

    public GameObject buyPrompt;
    public GameObject wonPrompt;
    public GameObject lostPrompt;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(gameObject);
    }

    public void ShowBuyPrompt()
    {
        buyPrompt.SetActive(true);
    }

    public void ShowWonPrompt()
    {
        wonPrompt.SetActive(true);
    }

    public void ShowLostPrompt()
    {
        lostPrompt.SetActive(true);
    }

    public void RemovePrompts()
    {
        buyPrompt.SetActive(false);
        wonPrompt.SetActive(false);
        lostPrompt.SetActive(false);
    }
}
