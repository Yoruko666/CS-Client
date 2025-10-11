using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadyState : IState
{
    public float readyTime;

    public ReadyState(FSMController FSM): base(FSM)
    {
    }

    public override void Update()
    {
        readyTime -= Time.deltaTime;
        if (readyTime <= 0)
            FSM.SwitchState(States.Idle);
    }
     
    public override void OnStateEnter()
    {
        FSM.weaponController.Ready();
        FSM.characterController.animator.Play("Ready");
        readyTime = FSM.weaponController.weaponConfig.readyTime;
        FSM.fireCold = 0;
    }
    public override void OnStateExit()
    {
    }
}
