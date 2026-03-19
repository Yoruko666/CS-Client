using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIKillBanner : MonoBehaviour
{
    public static UIKillBanner instance;

    public GameObject killNormal;
    public GameObject killHead;
    private RectTransform rectTransform;

    private void Awake()
    {
        if(instance == null)
            instance = this;
        else Destroy(instance);

        rectTransform = GetComponent<RectTransform>();
        killNormal.SetActive(false);
        killHead.SetActive(false);
    }

    public void ShowKillBanner(bool headshot)
    {
        StopAllCoroutines();
        if (headshot)
        {
            StartCoroutine(HideKillBanner(killHead));
        }
        else
        {
            StartCoroutine(HideKillBanner(killNormal));
        }
    }

    private IEnumerator HideKillBanner(GameObject banner)
    {
        banner.SetActive(true);
        Image image = banner.GetComponent<Image>();

        float timer = 0f;
        Vector2 currentPos = new(0, -400 - 40);
        rectTransform.anchoredPosition = currentPos;
        image.color = new Color(image.color.r, image.color.g, image.color.b, 0); 

        while (timer <= 0.25f)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / 0.1f);
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);

            if (timer <= 0.15f)
            {
                float shakeOffset = Mathf.Sin(timer * 80) * 15;
                currentPos = new Vector2(0, -400 + shakeOffset);
            }
            else
            {
                currentPos = Vector2.MoveTowards(currentPos, new Vector2(0, -400), Time.deltaTime * 500);
            }

            rectTransform.anchoredPosition = currentPos;
            yield return null;
        }

        timer = 0f;
        while (timer <= 1.5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;
        while (timer <= 0.4f)
        {
            timer += Time.deltaTime;
            float alpha = 1 - Mathf.Clamp01(timer / 0.4f);
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
            yield return null;
        }

        banner.SetActive(false);
    }
}