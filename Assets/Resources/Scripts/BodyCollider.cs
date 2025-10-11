using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyCollider : MonoBehaviour
{
    public Transform character;
    public BodyPart part;

    public void GetDamaged(float damage)
    {
        character.GetComponent<TPPlayerController>().GetDamaged(damage);
    }
}

public enum BodyPart
{
    Head, Torso, Legs
}