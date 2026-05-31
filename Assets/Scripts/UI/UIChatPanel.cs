using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class UIChatPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject inputBar;
    [SerializeField] private float padding = 10f;

    private bool open;
    private GameObject chatCellPrefab;
    private readonly List<GameObject> chatCellList = new();

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
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        {
            if (!open)
            {
                OnOpen();
            }
            else
            {
                OnClose();
            }
        }
    }

    private void OnOpen()
    {
        open = true;
        inputBar.SetActive(true);
        inputField.ActivateInputField();
        foreach (GameObject chatCell in chatCellList)
        {
            chatCell.SetActive(true);
        }
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;

        if (PlayerController.instance != null)
            PlayerController.instance.inputLocked = true;
    }

    private void OnClose()
    {
        open = false;
        inputBar.SetActive(false);
        foreach (GameObject chatCell in chatCellList)
        {
            chatCell.SetActive(false);
        }
        if (!string.IsNullOrEmpty(inputField.text))
        {
            NetworkManager.Send(MessageType.Chat, new Chat(NetworkManager.instance.uid, ChatArea.All, inputField.text));
            inputField.text = string.Empty;
        }

        if (PlayerController.instance != null)
            PlayerController.instance.inputLocked = false;
    }


    private void AddChatCell(Chat chat)
    {
        if (chatCellPrefab == null) return;
        GameObject chatCell = Instantiate(chatCellPrefab, scrollRect.content);
        chatCellList.Add(chatCell);

        Transform bg = chatCell.transform.Find("Bg");
        Transform text = bg.transform.Find("Text");

        TextMeshProUGUI tmpText = text.GetComponent<TextMeshProUGUI>();
        tmpText.text = (chat.area == ChatArea.Team ? "(Team)" : "(All)") + chat.uid + ": " + chat.text;

        LayoutRebuilder.ForceRebuildLayoutImmediate(text as RectTransform);

        float textHeight = (text as RectTransform).sizeDelta.y;
        float bgHeight = textHeight + padding * 2;

        RectTransform bgRect = bg as RectTransform;
        bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, bgHeight);

        RectTransform cellRect = chatCell.transform as RectTransform;
        cellRect.sizeDelta = new Vector2(cellRect.sizeDelta.x, bgHeight);

        if (open)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        StartCoroutine(HideChatCellInTime(chatCell));
    }

    private IEnumerator HideChatCellInTime(GameObject chatCell)
    {
        yield return new WaitForSecondsRealtime(5f);
        chatCell.SetActive(false);
    }
}
