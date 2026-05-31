using UnityEngine;

public class IdleState : IState
{
    public IdleState(FSMController FSM) : base(FSM)
    {
    }

    public override void Update()
    {
        if (PlayerController.instance != null && PlayerController.instance.inputLocked) return;

        var w = FSM.weaponController;
        var cfg = w.weaponConfig;

        bool reloadInput = Input.GetKeyDown(KeyCode.R) || w.ammoNum == 0;
        bool hasReserve = w.ammoReserve > 0;
        bool magNotFull = w.ammoNum < cfg.magazineCapacity;
        if (reloadInput && hasReserve && magNotFull)
        {
            FSM.SwitchState(States.Reload);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1) && cfg.hasAim)
        {
            FSM.SwitchState(States.Aim);
            return;
        }

        bool canFire = GameManager.instance.isMainScene && w.ammoNum > 0 && FSM.fireCold <= 0;
        bool firePressed = cfg.isAuto
            ? Input.GetKey(KeyCode.Mouse0)
            : Input.GetKeyDown(KeyCode.Mouse0);
        if (canFire && firePressed)
        {
            FSM.SwitchState(States.Fire);
        }
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
