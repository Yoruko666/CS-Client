using UnityEngine;

public class UIHitIndicatorRoot : MonoBehaviour
{
    private GameObject _hitIndicatorPrefab;

    private void Awake()
    {
        _hitIndicatorPrefab = Resources.Load<GameObject>("Prefabs/UI/HitIndicator");
    }

    private void OnEnable()
    {
        EventCenter.Subscribe<Hit>(GameEvent.LocalPlayerHit, OnLocalPlayerHit);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe<Hit>(GameEvent.LocalPlayerHit, OnLocalPlayerHit);
    }

    private void OnLocalPlayerHit(Hit hit)
    {
        if (_hitIndicatorPrefab == null) return;
        // TODO P2-4：HitIndicator 走对象池，进一步省去 Instantiate/Destroy 开销
        GameObject indicator = Instantiate(_hitIndicatorPrefab, transform);
        indicator.GetComponent<UIHitIndicator>().Initialize(hit.GetPosition());
    }
}
