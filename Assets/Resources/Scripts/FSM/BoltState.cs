using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoltState : IState
{
    private float timer;

    public BoltState(FSMController FSM) : base(FSM)
    {
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if(timer <= 0)
        {
            FSM.SwitchState(States.Idle);
        }
    }

    public override void OnStateEnter()
    {
        timer = 1.2f;
        FSM.characterController.animator.Play("Bolt");
        FSM.weaponController.Bolt();
    }
    public override void OnStateExit()
    {
    }
}
