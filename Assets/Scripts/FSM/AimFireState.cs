using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimFireState : IState
{
    float fireTime;

    public AimFireState(FSMController FSM) : base(FSM)
    {
    }

    public override void Update()
    {
        fireTime -= Time.deltaTime;
        if (fireTime <= 0)
            FSM.SwitchState(States.Aim);
    }

    public override void OnStateEnter()
    {
        FSM.characterController.animator.Play("Aim Fire");
        FSM.characterController.Fire();
        FSM.weaponController.Fire();
        fireTime = 0.05f;
        FSM.fireCold = 1 / FSM.weaponController.weaponConfig.shootSpeed;
    }
    public override void OnStateExit()
    {
    }
}
