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
        FSM.weaponManager.animator.Play("Aim Fire");
        FSM.weaponManager.Fire();
        FSM.weaponController.Fire();
        fireTime = 0.05f;
        FSM.fireCold = 1 / FSM.weaponController.weaponConfig.shootSpeed;
    }
    public override void OnStateExit()
    {
    }
}
