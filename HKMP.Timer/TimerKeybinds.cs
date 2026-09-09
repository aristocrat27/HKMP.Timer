using InControl;

namespace HKMP.Timer
{
    public sealed class TimerKeybinds : PlayerActionSet
    {
        public PlayerAction Timer;

        public TimerKeybinds()
        {
            Timer =
                CreatePlayerAction(
                    "Timer"
                );

            Timer.AddDefaultBinding(
                Key.F8
            );
        }
    }
}