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
        stateDictionary = new Dictionary<States, IState>();
        stateDictionary.Add(States.Ready, new ReadyState(this));
        stateDictionary.Add(States.Idle, new IdleState(this));
        stateDictionary.Add(States.Aim, new AimState(this));
        stateDictionary.Add(States.Reload, new ReloadState(this));
        stateDictionary.Add(States.Fire, new FireState(this));
        stateDictionary.Add(States.AimFire, new AimFireState(this));
        stateDictionary.Add(States.Bolt, new BoltState(this));
    }

    public void Update()
    {
        stateDictionary[currentState].Update();
        if(fireCold > 0)
            fireCold -= Time.deltaTime;
        if (currentState != States.Fire && currentState != States.AimFire)
            characterController.RecoilDown();
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
