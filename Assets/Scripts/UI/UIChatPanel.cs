using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class UIChatPanel : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float bubbleWidth = 300f;
    [SerializeField] private float padding = 10f;

    private GameObject chatCellPrefab;

    private void OnEnable()
    {
        EventCenter.Subscribe<Chat>(GameEvent.Chat, AddChatCell);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<Chat>(GameEvent.Chat, AddChatCell);
    }

    private void Start()
    {
        chatCellPrefab = Addressables.LoadAssetAsync<GameObject>("UIChatCell").WaitForCompletion();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            AddChatCell(new Chat(1234, ChatArea.Team, "2154154846984984"));
        }
    }

    private void AddChatCell(Chat chat)
    {
        if (chatCellPrefab == null) return;

        GameObject chatCell = Instantiate(chatCellPrefab, content);
        Transform bg = chatCell.transform.Find("Bg");
        Transform text = bg.transform.Find("Text");

        TextMeshProUGUI tmpText = text.GetComponent<TextMeshProUGUI>();
        tmpText.text = chat.uid + ": " + chat.text;

        LayoutRebuilder.ForceRebuildLayoutImmediate(text as RectTransform);

        float textHeight = (text as RectTransform).sizeDelta.y;
        float bgHeight = textHeight + padding * 2;

        RectTransform bgRect = bg as RectTransform;
        bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, bgHeight);

        RectTransform cellRect = chatCell.transform as RectTransform;
        cellRect.sizeDelta = new Vector2(cellRect.sizeDelta.x, bgHeight);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}
