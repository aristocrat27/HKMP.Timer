using System;

namespace HKMP.Timer
{
    public static class TimerIntegrationApi
    {
        public static bool IsAvailable
        {
            get
            {
                return
                    TimerServerManager.Instance != null;
            }
        }

        public static void SetRoundExpirationHandler(
            Action<uint> handler)
        {
            TimerServerManager.SetRoundExpirationHandler(
                handler);
        }

        public static bool StartRound(
            uint roundId)
        {
            TimerServerManager manager =
                TimerServerManager.Instance;

            if (manager == null)
            {
                return false;
            }

            return manager.StartForRound(
                roundId);
        }

        public static bool EndRound(
            uint roundId)
        {
            TimerServerManager manager =
                TimerServerManager.Instance;

            if (manager == null)
            {
                return false;
            }

            return manager.EndForRound(
                roundId);
        }
    }
}
