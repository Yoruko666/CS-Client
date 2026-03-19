using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimState : IState
{
    public AimState(FSMController FSM) : base(FSM)
    {
    }

    public override void Update()
    {
        if ((Input.GetKeyDown(KeyCode.R) || FSM.weaponController.ammoNum == 0) && FSM.weaponController.ammoReserve > 0 && FSM.weaponController.ammoNum < FSM.weaponController.weaponConfig.magazineCapacity)
            FSM.SwitchState(States.Reload);
        if (Input.GetKeyDown(KeyCode.Mouse1))
            FSM.SwitchState(States.Idle);
        if (GameManager.instance.isMainScene && FSM.weaponController.ammoNum > 0 && FSM.fireCold <= 0 && (Input.GetKey(KeyCode.Mouse0) && FSM.weaponController.weaponConfig.isAuto || Input.GetKeyDown(KeyCode.Mouse0) && !FSM.weaponController.weaponConfig.isAuto))
        {
            if(FSM.weaponController.weaponConfig.hasAimFire)
                FSM.SwitchState(States.AimFire);
            else FSM.SwitchState(States.Fire);
        }
    }

    public override void OnStateEnter()
    {
        FSM.weaponController.Idle();
        FSM.characterController.animator.CrossFadeInFixedTime("Aim", 0.1f);
    }

    public override void OnStateExit()
    {
    }
}
