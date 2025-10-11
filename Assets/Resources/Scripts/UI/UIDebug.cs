using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIDebug : MonoBehaviour
{
    public TextMeshProUGUI text;

    private void Update()
    {
        text.text = "Reconciliation times: " + NetworkManager.reconciliationTime.ToString();
    }
}
