using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
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
}
