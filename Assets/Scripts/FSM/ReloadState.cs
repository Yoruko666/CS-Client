using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class ReloadState : IState
{
    private float reloadTime;
    private ReloadStep currentStep;
    private bool reloading;

    public ReloadState(FSMController FSM) : base(FSM)
    {
    }

    public override void Update()
    {
        if (FSM.weaponController.weaponConfig.SingleReload)
            SingleReloadUpdate();
        else MagazineReloadUpdate();
    }

    public override void OnStateEnter()
    {
        PlayerReload playerReload = new(NetworkManager.instance.playerName);
        NetworkManager.SendMessage(MessageType.Reload, playerReload);
        if (FSM.weaponController.weaponConfig.SingleReload)
            SingleReloadEnter();
        else MagazineReloadEnter();
    }
    public override void OnStateExit()
    {

    }

    private void MagazineReloadEnter()
    {
        FSM.characterController.animator.Play("Reload");
        FSM.weaponController.MagazineReload();
        reloadTime = 0;
    }

    private void MagazineReloadUpdate()
    {
        reloadTime += Time.deltaTime;
        if (reloadTime >= FSM.weaponController.weaponConfig.reloadTime)
            FSM.SwitchState(States.Idle);
        if (reloadTime >= FSM.weaponController.weaponConfig.reloadTime)
            FSM.weaponController.ReloadDone();
    }

    private void SingleReloadEnter()
    {
        reloading = false;
        currentStep = ReloadStep.Open;
        FSM.characterController.animator.Play("ReloadOpen");
        FSM.weaponController.ReloadOpen();
    }

    private void SingleReloadUpdate()
    {
        switch (currentStep)
        {
            case ReloadStep.Open:
                if (FSM.characterController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.99f)
                    currentStep = ReloadStep.Insert;
                break;
            case ReloadStep.Insert:
                if (FSM.weaponController.ammoNum == FSM.weaponController.weaponConfig.magazineCapacity || FSM.weaponController.ammoReserve == 0)
                {
                    currentStep = ReloadStep.Close;
                    FSM.characterController.animator.Play("ReloadClose");
                    FSM.weaponController.ReloadClose();
                }
                else if (!reloading)
                {
                    reloading = true;
                    FSM.characterController.animator.PlayInFixedTime("ReloadInsert", 0, 0);
                    FSM.weaponController.ReloadInsert();
                }
                else if (FSM.characterController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.99f)
                {
                    FSM.weaponController.ammoNum++;
                    FSM.weaponController.ammoReserve--;
                    reloading = false;
                }
                break;
            case ReloadStep.Close:
                if (FSM.characterController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.99f)
                    FSM.SwitchState(States.Idle);
                break;
        }
    }

    private enum ReloadStep
    {
        Open, Insert, Close
    }
}