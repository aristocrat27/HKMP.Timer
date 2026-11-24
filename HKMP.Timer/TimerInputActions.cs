using InControl;

namespace HKMP.Timer
{
    public static class TimerInputActions
    {
        public static TimerKeybinds Keybinds
        {
            get
            {
                if (TimerMod.GlobalSettings == null)
                {
                    return null;
                }

                if (TimerMod.GlobalSettings.KeyBinds == null)
                {
                    TimerMod.GlobalSettings.KeyBinds =
                        new TimerKeybinds();
                }

                TimerMod.GlobalSettings.KeyBinds.EnsureActions();
                return TimerMod.GlobalSettings.KeyBinds;
            }
        }

        public static PlayerAction Timer =>
            Keybinds?.Timer;

        public static PlayerAction ToggleTimer =>
            Keybinds?.ToggleTimer;

        public static PlayerAction ShowStatus =>
            Keybinds?.ShowStatus;

        public static void Initialize()
        {
            if (TimerMod.GlobalSettings == null)
            {
                TimerMod.GlobalSettings =
                    new TimerGlobalSettings();
            }


            if (TimerMod.GlobalSettings.KeyBinds == null)
            {
                TimerMod.GlobalSettings.KeyBinds =
                    new TimerKeybinds();
            }

            TimerMod.GlobalSettings.KeyBinds.EnsureActions();
        }
    }
}