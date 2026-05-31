using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    private float mouseSensitive = 0.2f;

    private float moveInputX, moveInputY;
    private float lookInputX, lookInputY;

    private Vector3 position;
    private float rotationX, rotationY;

    public float speed = 0;
    private float targetSpeed = 0;
    private Vector3 movement;

    private readonly float SPEED_WALK = 3;
    private readonly float SPEED_RUN = 6;
    private readonly float SPEED_CROUCH = 2;
    private readonly float GRAVITY = 9.8f;

    private bool jump;
    private bool isWalk;
    private bool isCrouch;
    private bool isGrounded;
    private bool isInAir;
    public bool isDie = true;
    public bool inputLocked;
    private bool isRespawning = false;

    private float velocity = 0;
    private float height = 1.8f;

    public Transform arm;

    private CharacterController characterController;
    private Transform center;
    private Transform body;
    public PlayerInputControl inputControl;
    private PlayerState playerState;

    [HideInInspector] public PlayerStateInfo previousState, currentState;
    private float updateTime = 0;

    /// <summary>开始踉跄的最小下落速度阈值（绝对值）。低于此值的落地不踉跄。</summary>
    private const float STAGGER_FALL_THRESHOLD = 4f;
    /// <summary>下落速度对应踉跄强度的比例：每多 1 单位向下速度，多 0.06 秒踉跄。</summary>
    private const float STAGGER_DURATION_PER_SPEED = 0.06f;
    /// <summary>踉跄期最大时长（防止从超高处坠落卡死）。</summary>
    private const float STAGGER_DURATION_MAX = 0.6f;
    /// <summary>踉跄期的水平加速度（远低于正常 50/s²，玩家会"使不上劲"）。</summary>
    private const float STAGGER_ACCELERATION = 8f;
    /// <summary>踉跄期还剩多少秒（>0 表示当前正在踉跄）。同步在客户端 / 服务端两端。</summary>
    private float staggerTimer = 0f;

    // 镜头下压（仅本地视觉，不参与回滚）
    /// <summary>镜头当前的踉跄下压角度（度，正数表示低头）。</summary>
    private float landKickAngle = 0f;
    /// <summary>当前动量速度（用于阻尼弹簧回归 0）。</summary>
    private float landKickVelocity = 0f;
    /// <summary>镜头弹簧固有频率（越高回弹越快）。</summary>
    private const float LAND_KICK_FREQ = 8f;
    /// <summary>镜头弹簧阻尼比（1 = 临界阻尼，<1 会过冲，>1 缓慢）。</summary>
    private const float LAND_KICK_DAMPING = 0.9f;

    /// <summary>外部（如 WeaponManager）查询的当前镜头下压度数。</summary>
    public float LandKickAngle => landKickAngle;

    //Shared
    private readonly static float TICK_INTERVAL = NetworkManager.TICK_INTERVAL;

    private static readonly Vector3 VEC3_CROUCH_CENTER = new(0, 1.2f, 0);
    private static readonly Vector3 VEC3_NORMAL_CENTER = new(0, 1.6f, 0);
    private static readonly Vector3 VEC3_Y_UP = new(0, 1, 0);
    private static readonly Vector3 VEC3_ZERO = Vector3.zero;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(gameObject);

        characterController = GetComponent<CharacterController>();
        playerState = GetComponent<PlayerState>();
        center = transform.Find("Center");
        body = transform.Find("Body");
        inputControl = new PlayerInputControl();
        inputControl.Enable();
    }

    private void Update()
    {
        if (isRespawning) return;
        if (!isDie)
        {
            CollectInput();
            UpdateLandKick(Time.deltaTime);
            float alpha = (Time.time - updateTime) / TICK_INTERVAL;
            if ((previousState.GetPosition() - currentState.GetPosition()).magnitude > 1f)
            {
                characterController.enabled = false;
                transform.position = currentState.GetPosition();
                characterController.enabled = true;
            }
            else characterController.Move(Vector3.Lerp(previousState.GetPosition(), currentState.GetPosition(), alpha) - transform.position); 
        }
    }

    /// <summary>
    /// 落地踉跄镜头下压：产生一个瞬时下压角度，之后通过阻尼弹簧回归 0。
    /// 仅本地视觉效果，不参与服务端权威 / 回滚。
    /// </summary>
    private void TriggerLandKick(float staggerDuration)
    {
        // 下压角度：踉跄 0~0.6s 对应 ~1°~12°；用线性映射
        float kick = Mathf.Lerp(1f, 12f, staggerDuration / STAGGER_DURATION_MAX);
        // 取最大值：避免连续小落地把已经在恢复的踉跄重置成更小值
        landKickAngle = Mathf.Max(landKickAngle, kick);
        // 给一个负的初速度让弹簧"先继续往下蹲"再回弹（更有 punch 感）
        landKickVelocity = -kick * 8f;
    }

    /// <summary>
    /// 阻尼弹簧把 landKickAngle 拉回 0。
    /// 公式：x'' = -ω²·x - 2ζω·x'    （二阶系统经典写法）
    /// </summary>
    private void UpdateLandKick(float dt)
    {
        if (landKickAngle == 0f && landKickVelocity == 0f) return;
        float omega = LAND_KICK_FREQ;
        float accel = -omega * omega * landKickAngle - 2f * LAND_KICK_DAMPING * omega * landKickVelocity;
        landKickVelocity += accel * dt;
        landKickAngle += landKickVelocity * dt;
        if (Mathf.Abs(landKickAngle) < 0.01f && Mathf.Abs(landKickVelocity) < 0.5f)
        {
            landKickAngle = 0f;
            landKickVelocity = 0f;
        }
    }

    private void CollectInput()
    {
        if (inputLocked) return;
        Vector2 moveInput = inputControl.Gameplay.Move.ReadValue<Vector2>();
        moveInputX = moveInput.x;
        moveInputY = moveInput.y;
        Vector2 lookInput = inputControl.Gameplay.Look.ReadValue<Vector2>();
        if (GameManager.instance.isMainScene)
        {
            lookInputX += lookInput.x;
            lookInputY += lookInput.y;
        }
        jump |= Input.GetKeyDown(KeyCode.Space);
        isCrouch = Input.GetKey(KeyCode.LeftControl);
        isWalk = Input.GetKey(KeyCode.LeftShift);

        float currentRotationY = rotationY + lookInputX * mouseSensitive;
        float currentRotationX = Mathf.Clamp(rotationX - lookInputY * mouseSensitive, -60, 60);
        transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
        center.localRotation = Quaternion.Euler(currentRotationX, 0, 0);
        if (isCrouch) center.localPosition = Vector3.MoveTowards(center.localPosition, VEC3_CROUCH_CENTER, 4 * Time.deltaTime);
        else center.localPosition = Vector3.MoveTowards(center.localPosition, VEC3_NORMAL_CENTER, 4 * Time.deltaTime);
    }

    public PlayerStateInfo GetPlayerStateInfo()
    {
        PlayerStateInfo playerStateInfo = new()
        {
            positionX = position.x,
            positionY = position.y,
            positionZ = position.z,
            rotationX = rotationX,
            rotationY = rotationY,
            speed = speed,
            velocity = velocity,
            height = height,
            isCrouch = isCrouch
        };
        return playerStateInfo;
    }

    public void UpdatePlayerState(ref PlayerStateInfo playerState)
    {
        playerState.positionX = position.x;
        playerState.positionY = position.y;
        playerState.positionZ = position.z;
        playerState.rotationX = rotationX;
        playerState.rotationY = rotationY;
        playerState.speed = speed;
        playerState.velocity = velocity;
        playerState.height = height;
        playerState.isCrouch = isCrouch;
    }

    public PlayerInputInfo GetInputInfo()
    {
        PlayerInputInfo inputInfo = new(NetworkManager.instance.uid, moveInputX, moveInputY, lookInputX, lookInputY, jump, isWalk, isCrouch);
        lookInputX = 0;
        lookInputY = 0;
        jump = false;
        return inputInfo;
    }

    public void ApplyPlayerState(PlayerStateInfo playerState)
    {
        if (isDie || isRespawning) return;
        currentState = playerState;
        position = playerState.GetPosition();

        characterController.enabled = false;
        transform.position = new Vector3(playerState.positionX, playerState.positionY, playerState.positionZ);
        characterController.enabled = true;

        rotationY = playerState.rotationY;
        rotationX = playerState.rotationX;
        transform.rotation = Quaternion.Euler(0, playerState.rotationY, 0);
        center.transform.localRotation = Quaternion.Euler(playerState.rotationX, 0, 0);
        speed = playerState.speed;
        velocity = playerState.velocity;
        isCrouch = playerState.isCrouch;
        height = playerState.height;
        characterController.height = height;
        characterController.center = new Vector3(0, height / 2, 0);
    }

    public void ProcessInput(PlayerInputInfo inputInfo)
    {
        if (isRespawning) return;
         
        updateTime = Time.time;
        previousState = currentState;
        ApplyPlayerState(currentState);

        float moveInputX = inputInfo.moveInputX, moveInputY = inputInfo.moveInputY;
        float lookInputX = inputInfo.lookInputX, lookInputY = inputInfo.lookInputY;

        rotationY += lookInputX * mouseSensitive;
        rotationX -= lookInputY * mouseSensitive;
        rotationX = Mathf.Clamp(rotationX, -60, 60);
        transform.rotation = Quaternion.Euler(0, rotationY, 0);
        center.localRotation = Quaternion.Euler(rotationX, 0, 0);

        Vector3 direction = transform.rotation * new Vector3(moveInputX, 0, moveInputY);
        direction.y = 0;
        direction = direction.normalized;

        if (moveInputX != 0 || moveInputY != 0)
        {
            if (inputInfo.isCrouch) targetSpeed = SPEED_CROUCH;
            else if (inputInfo.isWalk) targetSpeed = SPEED_WALK;
            else targetSpeed = SPEED_RUN;
        }
        else targetSpeed = 0;

        // 加速度：地面正常 50，空中 15，踉跄期降到 STAGGER_ACCELERATION（玩家"使不上劲"）
        // 踉跄期计时与状态保持在 staggerTimer，每 tick 衰减
        float acceleration;
        if (isGrounded)
            acceleration = staggerTimer > 0f ? STAGGER_ACCELERATION : 50f;
        else
            acceleration = 15f;
        speed = Mathf.MoveTowards(speed, targetSpeed, acceleration * TICK_INTERVAL);
        movement = Vector3.MoveTowards(movement, direction * targetSpeed, acceleration * TICK_INTERVAL);

        if (inputInfo.jump && isGrounded && !inputInfo.isCrouch)
        {
            isGrounded = false;
            velocity = 4;
        }
        characterController.Move((movement + new Vector3(0, velocity, 0)) * TICK_INTERVAL);

        if (characterController.isGrounded)
        {
            isGrounded = true;
            // 落地的瞬间：根据落地前下落速度（velocity 负数）触发踉跄
            if (isInAir)
            {
                isInAir = false;
                float fallSpeed = -velocity;        // 转正
                if (fallSpeed > STAGGER_FALL_THRESHOLD)
                {
                    float over = fallSpeed - STAGGER_FALL_THRESHOLD;
                    float duration = Mathf.Min(over * STAGGER_DURATION_PER_SPEED, STAGGER_DURATION_MAX);
                    // 取最大值：连续两次踉跄不会被新一次的小落地打断
                    if (duration > staggerTimer) staggerTimer = duration;
                    // 触发镜头下压（仅本地视觉，下压度数与时长成正比）
                    TriggerLandKick(duration);
                }
            }
            velocity = -0.5f;
        }
        else
        {
            isInAir = true;
            velocity -= GRAVITY * TICK_INTERVAL;
        }

        // 踉跄计时衰减（必须在客户端 / 服务端两端同样运行）
        if (staggerTimer > 0f) staggerTimer = Mathf.Max(0f, staggerTimer - TICK_INTERVAL);

        if (inputInfo.isCrouch) height = Mathf.MoveTowards(height, 1.2f, 4 * TICK_INTERVAL);
        else height = Mathf.MoveTowards(height, 1.6f, 4 * TICK_INTERVAL);
        characterController.height = height;
        characterController.center = new Vector3(0, height / 2, 0);

        position = transform.position;
        currentState = GetPlayerStateInfo();
    }

    public void Die()
    {
        isDie = true;
        characterController.enabled = false;
        center.gameObject.SetActive(false);
        body.gameObject.SetActive(true);
        StartCoroutine(FallDown());

        // 标记本回合死过：下次 Initialize 时由 WeaponManager 丢主武器
        var wm = GetComponent<WeaponManager>();
        if (wm != null) wm.diedThisRound = true;
    }

    public IEnumerator FallDown()
    {
        yield return null;
        body.GetComponent<Animator>().enabled = false;
    }

    public void Initialize()
    {
        isRespawning = true;
        StopAllCoroutines();

        int slot = NetworkManager.instance.slot;
        isDie = false;
        characterController.enabled = false;

        speed = 0;
        targetSpeed = 0;
        velocity = 0;
        movement = Vector3.zero;
        isGrounded = false;
        isInAir = false;
        jump = false;
        isCrouch = false;
        height = 1.8f;
        // 清空踉跄状态，避免上一回合的镜头下压残留到重生瞬间
        staggerTimer = 0f;
        landKickAngle = 0f;
        landKickVelocity = 0f;
        characterController.height = height;
        characterController.center = new Vector3(0, height / 2, 0);

        rotationX = 0;
        rotationY = (slot / 3) * 180;
        transform.position = MatchManager.instance.mapConfig.bornPoints[slot];
        transform.rotation = Quaternion.Euler(0, ((slot / 3) * 180), 0);
        position = transform.position;

        characterController.enabled = true;
        center.gameObject.SetActive(true);
        body.GetComponent<Animator>().enabled = true;
        body.gameObject.SetActive(false);

        previousState = GetPlayerStateInfo();
        currentState = GetPlayerStateInfo();
        updateTime = Time.time;

        StartCoroutine(EndRespawnCoroutine());
    }
    private IEnumerator EndRespawnCoroutine()
    {
        yield return new WaitForEndOfFrame();
        isRespawning = false;
    }
}