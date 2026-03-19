using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIHitIndicator : MonoBehaviour
{
    private Vector3 position, direction, forward;
    private float timer = 1f;
    private RectTransform rectTransform;
    private Transform player;
    private Vector2 UIDirection;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        player = NetworkManager.instance.localPlayer.transform;
    }

    void Update()
    {
        direction = position - player.position;
        direction.y = 0;

        forward = player.forward;
        forward.y = 0;

        float angle = Vector3.SignedAngle(forward, direction, Vector3.up);
        UIDirection = Quaternion.Euler(0, 0, -angle) * Vector2.up;

        rectTransform.anchoredPosition = UIDirection * 482.5f;
        rectTransform.rotation = Quaternion.Euler(0, 0, -angle);

        timer -= Time.deltaTime;
        if(timer <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(Vector3 position)
    {
        this.position = position;
    }
}
