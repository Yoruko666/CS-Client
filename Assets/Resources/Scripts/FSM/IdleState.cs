using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : IState
{
    public IdleState(FSMController FSM) : base(FSM)
    {
    }

    public override void Update()
    {
        if ((Input.GetKeyDown(KeyCode.R) || FSM.weaponController.ammoNum == 0) && FSM.weaponController.ammoReserve > 0 && FSM.weaponController.ammoReserve > 0 && FSM.weaponController.ammoNum < FSM.weaponController.weaponConfig.magazineCapacity) 
            FSM.SwitchState(States.Reload);
        if (Input.GetKeyDown(KeyCode.Mouse1) && FSM.weaponController.weaponConfig.hasAim)
            FSM.SwitchState(States.Aim);
        if (GameManager.instance.isMainScene && FSM.weaponController.ammoNum > 0 && FSM.fireCold <= 0 && (Input.GetKey(KeyCode.Mouse0) && FSM.weaponController.weaponConfig.isAuto || Input.GetKeyDown(KeyCode.Mouse0) && !FSM.weaponController.weaponConfig.isAuto))
            FSM.SwitchState(States.Fire);
    }

    public override void OnStateEnter()
    {
        FSM.characterController.animator.CrossFadeInFixedTime("Idle", 0.1f);
        FSM.weaponController.Idle();
    }
    public override void OnStateExit()
    {
    }
}
