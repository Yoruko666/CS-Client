using System.Collections.Generic;
using UnityEngine;

public class TPPlayerController : MonoBehaviour
{
    public Transform spine;
    private Transform center;

    [HideInInspector] public Vector3 targetPosition;
    [HideInInspector] public float targetRotationY;
    [HideInInspector] public float targetRotationX;
    [HideInInspector] public float speed;
    [HideInInspector] public bool isCrouch;

    private bool isDead;
    private Rigidbody[] rigidbodies;
    private Collider[] colliders;

    private Animator animator;
    private CharacterController characterController;

    public int uid;
    public int slot;

    // ============ Snapshot 插值 ============

    /// <summary>
    /// 渲染延迟（秒）。客户端始终渲染 INTERP_DELAY 秒前的状态，
    /// 这段缓冲用于吸收网络抖动。
    /// 取值参考：CS:GO 31ms，TF2 100ms，Valorant ~120ms。
    /// 128Hz 下 40ms ≈ 5 个 server tick，是竞技 FPS 与抗抖动的折中。
    /// </summary>
    private const float INTERP_DELAY = 0.04f;

    /// <summary>缓冲队列最大长度（保护内存，正常 5~10 个就够用）。</summary>
    private const int SNAPSHOT_BUFFER_CAPACITY = 32;

    /// <summary>队列里 snapshot 比当前时间老多少秒就丢弃（脏数据保护）。</summary>
    private const float SNAPSHOT_MAX_AGE = 1.0f;

    /// <summary>外推的最大时长（秒）。超过这段时间没新 snapshot 就停止外推，避免漂移。</summary>
    private const float MAX_EXTRAPOLATION = 0.15f;

    /// <summary>一帧 snapshot 数据。</summary>
    private struct Snapshot
    {
        public float time;          // 接收时刻（Time.time）
        public Vector3 position;
        public float rotationY;
        public float rotationX;
        public float speed;
        public bool isCrouch;
    }

    /// <summary>
    /// 时间排序的 snapshot 队列（队首最老，队尾最新）。
    /// 用 List 而不是 Queue：需要随机访问以查找插值区间，但插入/删除都在两端，性能足够。
    /// </summary>
    private readonly List<Snapshot> _snapshots = new(SNAPSHOT_BUFFER_CAPACITY);

    /// <summary>
    /// 接收一帧服务端 snapshot，入队。
    /// 由 NetworkManager.OnAllPlayersInfo 在每次收到状态时调用。
    /// </summary>
    public void EnqueueSnapshot(PlayerStateInfo info)
    {
        if (isDead) return;
        var s = new Snapshot
        {
            time = Time.time,
            position = info.GetPosition(),
            rotationY = info.rotationY,
            rotationX = info.rotationX,
            speed = info.speed,
            isCrouch = info.isCrouch,
        };

        // 队列容量保护：超过则丢最老的
        if (_snapshots.Count >= SNAPSHOT_BUFFER_CAPACITY)
            _snapshots.RemoveAt(0);
        _snapshots.Add(s);
    }

    private void Update()
    {
        if (_snapshots.Count == 0)
        {
            // 还没收到任何状态，保持初始化的位置 / 旋转
            ApplyToTransform();
            return;
        }

        float renderTime = Time.time - INTERP_DELAY;

        // 清理过老的 snapshot（同时回收容量）
        while (_snapshots.Count > 2 && _snapshots[0].time < Time.time - SNAPSHOT_MAX_AGE)
            _snapshots.RemoveAt(0);

        // 在队列中找前后两帧使 a.time <= renderTime <= b.time
        Snapshot a = _snapshots[0];
        Snapshot b = _snapshots[_snapshots.Count - 1];
        bool found = false;
        for (int i = 0; i < _snapshots.Count - 1; i++)
        {
            if (_snapshots[i].time <= renderTime && renderTime <= _snapshots[i + 1].time)
            {
                a = _snapshots[i];
                b = _snapshots[i + 1];
                found = true;
                break;
            }
        }

        if (found)
        {
            // 正常插值：(a.time, b.time) 之间
            float span = b.time - a.time;
            float alpha = span > 1e-5f ? (renderTime - a.time) / span : 1f;
            ApplyInterpolated(a, b, alpha);
        }
        else if (renderTime < a.time)
        {
            // renderTime 比所有 snapshot 都早（刚连入第一帧），直接吃最老的
            ApplyDirect(a);
        }
        else
        {
            // renderTime 比最新 snapshot 还新（断包/网络抖动）
            // 短暂外推：用最近两帧速度推算；超过 MAX_EXTRAPOLATION 后冻结在最新位置
            float overshoot = renderTime - b.time;
            if (overshoot < MAX_EXTRAPOLATION && _snapshots.Count >= 2)
            {
                Snapshot prev = _snapshots[_snapshots.Count - 2];
                float span = b.time - prev.time;
                float alpha = span > 1e-5f ? (renderTime - prev.time) / span : 1f;
                ApplyInterpolated(prev, b, alpha);
            }
            else
            {
                ApplyDirect(b);
            }
        }

        ApplyToTransform();

        animator.SetFloat("Speed", speed);
        animator.SetBool("Crouch", isCrouch);
    }

    private void ApplyDirect(Snapshot s)
    {
        targetPosition = s.position;
        targetRotationY = s.rotationY;
        targetRotationX = s.rotationX;
        speed = s.speed;
        isCrouch = s.isCrouch;
    }

    private void ApplyInterpolated(Snapshot a, Snapshot b, float alpha)
    {
        targetPosition = Vector3.LerpUnclamped(a.position, b.position, alpha);
        // 角度用 LerpAngle 处理 360° 边界，避免转身时回头
        targetRotationY = Mathf.LerpAngle(a.rotationY, b.rotationY, alpha);
        targetRotationX = Mathf.LerpAngle(a.rotationX, b.rotationX, alpha);
        // 标量字段用最新值即可（动画状态、蹲伏不需要插值）
        speed = b.speed;
        isCrouch = b.isCrouch;
    }

    private void ApplyToTransform()
    {
        transform.position = targetPosition;
        transform.rotation = Quaternion.Euler(0, targetRotationY, 0);
    }

    private void LateUpdate()
    {
        if (!isDead)
        {
            spine.Rotate(0, 0, targetRotationX, Space.Self);
            center.localRotation = Quaternion.Euler(targetRotationX, 0, 0);
        }
    }

    public void Turn(float rotationY, float rotationX)
    {
        targetRotationY = rotationY;
        targetRotationX = rotationX;
    }

    /// <summary>
    /// 已废弃：现在通过 EnqueueSnapshot 提交服务端状态，由 Update 做插值。
    /// 保留方法以兼容旧调用，但只把数据丢入队列。
    /// </summary>
    public void ApplyPlayerState(PlayerStateInfo playerStateInfo)
    {
        EnqueueSnapshot(playerStateInfo);
    }

    public void GetDamaged(float damage)
    {
    }

    public void Initialize(int uid, int slot)
    {
        this.uid = uid;
        this.slot = slot;

        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        center = transform.Find("Center");
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();

        Initialize();
    }

    public void Initialize()
    {
        characterController.enabled = false;
        transform.position = MatchManager.instance.mapConfig.bornPoints[slot];
        transform.rotation = Quaternion.Euler(0, ((slot / 3) * 180), 0);
        isDead = false;
        animator.enabled = true;
        characterController.enabled = true;
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("CharacterController"))
                continue;
            collider.isTrigger = true;
        }
        foreach (Rigidbody rigidbody in rigidbodies)
            rigidbody.isKinematic = true;

        // 重置插值状态：避免上一回合的 snapshot 影响重生
        _snapshots.Clear();
        targetPosition = transform.position;
        targetRotationY = transform.rotation.eulerAngles.y;
        targetRotationX = 0;
        speed = 0;
        isCrouch = false;
    }

    public void Die()
    {
        isDead = true;
        animator.enabled = false;
        characterController.enabled = false;
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("CharacterController"))
                continue;
            collider.isTrigger = false;
        }
        foreach (Rigidbody rigidbody in rigidbodies)
            rigidbody.isKinematic = false;
    }
}

