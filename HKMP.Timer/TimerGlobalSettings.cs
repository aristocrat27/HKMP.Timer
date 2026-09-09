using Modding.Converters;
using Newtonsoft.Json;
using System;

namespace HKMP.Timer
{
    [Serializable]
    public class TimerGlobalSettings
    {
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public TimerKeybinds KeyBinds =
            new TimerKeybinds();

        public int TimerColor = 0;

        public float TimerX = -1f;
        public float TimerY = -1f;

        public float TimerWidth = 360f;
        public float TimerHeight = 100f;
    }
}