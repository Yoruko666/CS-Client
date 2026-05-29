using UnityEngine;
using UnityEngine.AddressableAssets;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    private AudioSource audioSource;
    private AudioClip _killAudio;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        EventCenter.Subscribe<PlayerKill>(GameEvents.PlayerKilled, OnPlayerKilled);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<PlayerKill>(GameEvents.PlayerKilled, OnPlayerKilled);
    }

    public void PlayAudio(AudioClip audioClip)
    {
        if (audioClip != null) audioSource.PlayOneShot(audioClip);
    }

    private void OnPlayerKilled(PlayerKill k)
    {
        // 只有"我"是击杀者时播放击杀音效
        if (NetworkManager.instance == null
            || k.playerKillName != NetworkManager.instance.playerName) return;

        // 懒加载（避免在 hall 阶段就拉资源；同步加载，第一次击杀会有微小卡顿）
        if (_killAudio == null)
            _killAudio = Addressables.LoadAssetAsync<AudioClip>("Kill1").WaitForCompletion();
        PlayAudio(_killAudio);
    }
}

