using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
    private bool isRespawning = false;

    private float velocity = 0;
    private float height = 1.8f;

    public Transform arm;

    private CharacterController characterController;
    private Transform center;
    private Transform body;
    private PlayerInputControl inputControl;
    private PlayerState playerState;

    [HideInInspector] public PlayerStateInfo previousState, currentState;
    private float updateTime = 0;

    //Shared
    private readonly static float TICK_INTERVAL = NetworkManager.TICK_INTERVAL;

    private static readonly Vector3 VEC3_CROUCH_CENTER = new(0, 1.2f, 0);
    private static readonly Vector3 VEC3_NORMAL_CENTER = new(0, 1.6f, 0);
    private static readonly Vector3 VEC3_Y_UP = new(0, 1, 0);
    private static readonly Vector3 VEC3_ZERO = Vector3.zero;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
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
            if (isDie) Initialize();
            CollectInput();
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

    private void CollectInput()
    {
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
        PlayerInputInfo inputInfo = new(NetworkManager.instance.playerName, moveInputX, moveInputY, lookInputX, lookInputY, jump, isWalk, isCrouch);
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

        int acceleration = isGrounded ? 50 : 15;
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
            velocity = -0.5f;
            if (isInAir)
            {
                isInAir = false;
                speed = 0;
            }
        }
        else
        {
            isInAir = true;
            velocity -= GRAVITY * TICK_INTERVAL;
        }

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

        int id = NetworkManager.instance.id;
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
        characterController.height = height;
        characterController.center = new Vector3(0, height / 2, 0);

        rotationX = 0;
        rotationY = (id / 3) * 180;
        transform.position = MatchManager.instance.mapConfig.bornPoints[id];
        transform.rotation = Quaternion.Euler(0, ((id / 3) * 180), 0);
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