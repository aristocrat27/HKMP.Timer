using InControl;

namespace HKMP.Timer
{
    public sealed class TimerKeybinds : PlayerActionSet
    {
        public PlayerAction Timer;

        public PlayerAction ToggleTimer;

        public PlayerAction ShowStatus;

        private PlayerAction _legacyStartTimer;

        private PlayerAction _legacyStopTimer;

        public TimerKeybinds()
        {
            EnsureActions();
        }

        public void EnsureActions()
        {
            if (Timer == null)
            {
                Timer = CreateAction("Timer", Key.F8);
            }

            if (ToggleTimer == null)
            {
                ToggleTimer = CreateAction("ToggleTimer", Key.F9);
            }

            if (_legacyStartTimer == null)
            {
                _legacyStartTimer = CreatePlayerAction("StartTimer");
            }

            if (_legacyStopTimer == null)
            {
                _legacyStopTimer = CreatePlayerAction("StopTimer");
            }

            MigrateLegacyBindings();

            if (ShowStatus == null)
            {
                ShowStatus = CreateAction("ShowStatus", Key.F7);
            }
        }

        private PlayerAction CreateAction(
            string actionName,
            Key defaultKey)
        {
            PlayerAction action =
                CreatePlayerAction(actionName);

            action.AddDefaultBinding(defaultKey);
            return action;
        }

        private void MigrateLegacyBindings()
        {
            bool hasStartBinding =
                _legacyStartTimer.Bindings.Count > 0;

            bool hasStopBinding =
                _legacyStopTimer.Bindings.Count > 0;

            if (!hasStartBinding && !hasStopBinding)
            {
                return;
            }

            PlayerAction legacyAction = _legacyStartTimer;

            if (!hasStartBinding ||
                (hasStopBinding &&
                 HasDefaultBinding(_legacyStartTimer, Key.F9) &&
                 !HasDefaultBinding(_legacyStopTimer, Key.F10)))
            {
                legacyAction = _legacyStopTimer;
            }

            if (ToggleTimer.Bindings.Count == 0 ||
                HasDefaultBinding(ToggleTimer, Key.F9))
            {
                BindingSource[] bindings =
                    new BindingSource[legacyAction.Bindings.Count];

                legacyAction.Bindings.CopyTo(bindings, 0);

                ToggleTimer.ClearBindings();
                _legacyStartTimer.ClearBindings();
                _legacyStopTimer.ClearBindings();

                foreach (BindingSource binding in bindings)
                {
                    ToggleTimer.AddBinding(binding);
                }
            }
            else
            {
                _legacyStartTimer.ClearBindings();
                _legacyStopTimer.ClearBindings();
            }
        }

        private static bool HasDefaultBinding(
            PlayerAction action,
            Key key)
        {
            if (action.Bindings.Count != 1)
            {
                return false;
            }

            KeyBindingSource binding =
                action.Bindings[0] as KeyBindingSource;

            return binding != null &&
                   binding.Control.IncludeCount == 1 &&
                   binding.Control.GetInclude(0) == key;
        }
    }
}
