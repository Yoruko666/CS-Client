using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float mouseSensitive = 0.2f;

    private float moveInputX, moveInputY;
    private float lookInputX, lookInputY;

    private Vector3 position;
    private float rotationX, rotationY;

    private float speed = 0;
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
    private readonly static float TICK_INTERVAL = 1f / 30f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerState = GetComponent<PlayerState>();
        center = transform.Find("Center");
        body = transform.Find("Body");
        inputControl = new PlayerInputControl();
        inputControl.Enable();
        position = transform.position;
        currentState = GetPlayerStateInfo();
        previousState = GetPlayerStateInfo();
    }

    private void Update()
    {
        if (playerState.HP > 0)
        {
            CollectInput();
            float alpha = (Time.time - updateTime) / TICK_INTERVAL;
            characterController.Move(Vector3.Lerp(previousState.GetPosition(), currentState.GetPosition(), alpha) - transform.position); 
        }
        else
        {
            Die();
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
        if (isCrouch) center.localPosition = Vector3.MoveTowards(center.localPosition, new Vector3(0, 1.2f, 0), 4 * Time.deltaTime);
        else center.localPosition = Vector3.MoveTowards(center.localPosition, new Vector3(0, 1.6f, 0), 4 * Time.deltaTime);
    }

    public PlayerStateInfo GetPlayerStateInfo()
    {
        PlayerStateInfo playerStateInfo = new PlayerStateInfo();
        playerStateInfo.positionX = position.x;
        playerStateInfo.positionY = position.y;
        playerStateInfo.positionZ = position.z;
        playerStateInfo.rotationX = rotationX;
        playerStateInfo.rotationY = rotationY;
        playerStateInfo.speed = speed;
        playerStateInfo.velocity = velocity;
        playerStateInfo.height = height;
        playerStateInfo.isCrouch = isCrouch;
        return playerStateInfo;
    }
    public void UpdateStateInfo(PlayerStateInfo playerStateInfo)
    {
        playerStateInfo.positionX = transform.position.x;
        playerStateInfo.positionY = transform.position.y;
        playerStateInfo.positionZ = transform.position.z;
        playerStateInfo.rotationY = rotationY;
        playerStateInfo.rotationX = rotationX;
        playerStateInfo.speed = speed;
        playerStateInfo.velocity = velocity;
        playerStateInfo.isCrouch = isCrouch;
    }


    public PlayerInputInfo GetInputInfo()
    {
        PlayerInputInfo inputInfo = new PlayerInputInfo(NetworkManager.instance.playerName, moveInputX, moveInputY, lookInputX, lookInputY, jump, isWalk, isCrouch);
        lookInputX = 0;
        lookInputY = 0;
        jump = false;
        return inputInfo;
    }

    public void ApplyPlayerState(PlayerStateInfo playerState)
    {
        currentState = playerState;
        position = playerState.GetPosition();
        characterController.Move(new Vector3(playerState.positionX, playerState.positionY, playerState.positionZ) - transform.position);
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
        updateTime = Time.time;
        previousState = new PlayerStateInfo(currentState);
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
        speed = Mathf.MoveTowards(speed, targetSpeed, 50 * TICK_INTERVAL);
        movement = Vector3.MoveTowards(movement, direction * targetSpeed, 50 * TICK_INTERVAL);

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
        transform.position = new Vector3(0, 0, -10); 
        transform.rotation = Quaternion.identity;
        characterController.enabled = true;
        center.gameObject.SetActive(true);
        body.GetComponent<Animator>().enabled = true;
        body.gameObject.SetActive(false);
    }
}