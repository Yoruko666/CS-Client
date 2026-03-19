using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPlayerPointer : MonoBehaviour
{
    public Transform player;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = transform.GetComponent<RectTransform>();
    }

    void Update()
    {
        rectTransform.anchoredPosition = new Vector3(player.position.x * 12.5f, player.position.z * 12.5f, 0);
        rectTransform.rotation = Quaternion.Euler(0, 0, -player.rotation.eulerAngles.y);
    }
}
