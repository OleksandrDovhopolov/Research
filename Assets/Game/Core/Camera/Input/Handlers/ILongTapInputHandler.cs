namespace InputSystem
{
    public interface ILongTapInputHandler : IInputHandler
    {
        public void OnStart(float activationTime);
        public void OnActivation(float activationTime, float progress);
        public void OnActivate();
        public void OnEnd(float activationTime, float progress);
    }
}