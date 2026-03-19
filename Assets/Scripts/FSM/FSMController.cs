using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FSMController
{
    private Dictionary<States, IState> stateDictionary;
    private States currentState;
    public WeaponController weaponController;
    public WeaponManager characterController;

    public float fireCold = 0;

    public FSMController(WeaponManager characterController)
    {
        this.characterController = characterController;
        stateDictionary = new Dictionary<States, IState>
        {
            { States.Ready, new ReadyState(this) },
            { States.Idle, new IdleState(this) },
            { States.Aim, new AimState(this) },
            { States.Reload, new ReloadState(this) },
            { States.Fire, new FireState(this) },
            { States.AimFire, new AimFireState(this) },
            { States.Bolt, new BoltState(this) }
        };
    }

    public void Update()
    {
        if(PlayerController.instance.isDie) return;
        stateDictionary[currentState].Update();
        if(fireCold > 0)
            fireCold -= Time.deltaTime;
    }

    public void Initialize(GameObject weapon)
    {
        weaponController = weapon.GetComponent<WeaponController>();
        currentState = States.Ready;
        SwitchState(States.Ready);
    }

    public void SwitchState(States newState)
    {
        stateDictionary[currentState].OnStateExit();
        currentState = newState;
        stateDictionary[currentState].OnStateEnter();

        if (newState == States.Aim || newState == States.AimFire)
            characterController.AimEnter(weaponController.weaponConfig.zoom);
        else 
            characterController.AimExit();
    }
}

public enum States
{
    Ready, Idle, Aim, Reload, Fire, AimFire, Bolt
}
