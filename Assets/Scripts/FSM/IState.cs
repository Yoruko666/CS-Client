public abstract class IState
{
    protected FSMController FSM;

    public IState(FSMController FSM)
    {
        this.FSM = FSM;
    }

    public abstract void Update();
    public abstract void OnStateEnter();
    public abstract void OnStateExit();
}
