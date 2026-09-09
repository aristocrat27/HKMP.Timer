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

                return TimerMod.GlobalSettings.KeyBinds;
            }
        }

        public static PlayerAction Timer
        {
            get
            {
                TimerKeybinds keybinds = Keybinds;

                if (keybinds == null)
                {
                    return null;
                }

                return keybinds.Timer;
            }
        }

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
        }
    }
}