using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIStart : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,  IPointerClickHandler
{
    private Image image;
    private TextMeshProUGUI text;

    private bool clicked = false;
    private float timer;

    private Color colorBgNormal = new Color(221f / 255f, 58f / 255f, 71f / 255f);
    private Color colorBgHighlight = new Color(236f / 255f, 232f / 255f, 225f / 255f);
    private Color colorBgClick = new Color(15f / 255f, 25f / 255f, 35f / 255f);

    private Color colorTextNormal = new Color(236f / 255f, 232f / 255f, 225f / 255f);
    private Color colorTextHighlight = new Color(15f / 255f, 25f / 255f, 35f / 255f);
    private Color colorTextClick = new Color(236f / 255f, 232f / 255f, 225f / 255f);


    void Start()
    {
        image = GetComponent<Image>();
        text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
    }
    
    void Update()
    {
        if (clicked)
        {
            timer += Time.deltaTime;
            text.text = $"{(int)timer / 60}:{(int)timer % 60}";
        }
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        if (!clicked)
        {
            image.color = colorBgHighlight;
            text.color = colorTextHighlight;
        }
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        if (!clicked)
        {
            image.color = colorBgNormal;
            text.color = colorTextNormal;
        }
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if (!clicked)
        {
            image.color = colorBgClick;
            text.color = colorTextClick;
            clicked = true;
            timer = 0;
        }
    }
}
