using UnityEngine;

public class FireState : IState
{
    float fireTime;

    public FireState(FSMController FSM) : base(FSM)
    {
    }

    public override void Update()
    {
        fireTime -= Time.deltaTime;
        if (fireTime <= 0)
        {
            if (FSM.weaponController.weaponConfig.hasBolt)
                FSM.SwitchState(States.Bolt);
            else FSM.SwitchState(States.Idle);
        }
    }

    public override void OnStateEnter()
    {
        FSM.weaponManager.animator.Play("Fire");
        FSM.weaponManager.Fire();
        FSM.weaponController.Fire();
        fireTime = FSM.weaponController.weaponConfig.fireTime;
        FSM.fireCold = 1 / FSM.weaponController.weaponConfig.shootSpeed;
    }
    public override void OnStateExit()
    {
    }
}
