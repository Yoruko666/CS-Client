using System.Collections;
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

    private void Start()
    {
        animator = GetComponent<Animator>();
        center = transform.Find("Center");
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        Initialize();
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.3f);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, targetRotationY, 0), 0.3f);
        animator.SetFloat("Speed", speed);
        animator.SetBool("Crouch", isCrouch);
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

    public void ApplyPlayerState(PlayerStateInfo playerStateInfo)
    {
        if (isDead) return;
        targetPosition = playerStateInfo.GetPosition();
        Turn(playerStateInfo.rotationY, playerStateInfo.rotationX);
        speed = playerStateInfo.speed;
        isCrouch = playerStateInfo.isCrouch;
    }

    public void GetDamaged(float damage)
    {
    }

    public void Initialize()
    {
        transform.position = new Vector3(0, 0, -10);
        transform.rotation = Quaternion.identity;
        isDead = false;
        animator.enabled = true;
        GetComponent<CharacterController>().enabled = true;
        foreach (Collider collider in colliders)
            collider.isTrigger = true;
        foreach (Rigidbody rigidbody in rigidbodies)
            rigidbody.isKinematic = true;
    }

    public void Die()
    {
        isDead = true;
        animator.enabled = false;
        GetComponent<CharacterController>().enabled = false;
        int layer = LayerMask.NameToLayer("Player");
        foreach (Collider collider in colliders)
            collider.isTrigger = false;
        foreach (Rigidbody rigidbody in rigidbodies)
            rigidbody.isKinematic = false;
    }
}
