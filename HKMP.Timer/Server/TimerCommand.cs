using Hkmp.Api.Command.Server;

namespace HKMP.Timer
{
    public sealed class TimerCommand : IServerCommand
    {
        private readonly TimerServerManager _manager;

        public TimerCommand(
            TimerServerManager manager)
        {
            _manager = manager;
        }

        public string Trigger
        {
            get
            {
                return "/timer";
            }
        }

        public string[] Aliases
        {
            get
            {
                return new[]
                {
                    "/tm"
                };
            }
        }

        public bool AuthorizedOnly
        {
            get
            {
                return true;
            }
        }

        public void Execute(
            ICommandSender commandSender,
            string[] args)
        {
            if (args == null ||
                args.Length < 2)
            {
                SendUsage(commandSender);
                return;
            }

            string action =
                args[1].ToLowerInvariant();

            switch (action)
            {
                case "start":
                    _manager.Start(
                        commandSender.SendMessage
                    );
                    break;

                case "stop":
                    _manager.Stop(
                        commandSender.SendMessage
                    );
                    break;

                case "time":
                    ExecuteTime(
                        commandSender,
                        args
                    );
                    break;

                case "status":
                    commandSender.SendMessage(
                        _manager.GetStatusMessage()
                    );
                    break;

                default:
                    SendUsage(commandSender);
                    break;
            }
        }

        private void ExecuteTime(
            ICommandSender commandSender,
            string[] args)
        {
            if (args.Length < 3)
            {
                commandSender.SendMessage(
                    "Usage: /timer time <seconds>"
                );

                return;
            }

            int seconds;

            if (!int.TryParse(
                    args[2],
                    out seconds))
            {
                commandSender.SendMessage(
                    "Error: time must be a whole number of seconds."
                );

                return;
            }

            if (seconds < 0)
            {
                commandSender.SendMessage(
                    "Error: time cannot be negative."
                );

                return;
            }

            _manager.SetDuration(
                seconds,
                commandSender.SendMessage
            );
        }

        private static void SendUsage(
            ICommandSender commandSender)
        {
            commandSender.SendMessage(
                "Usage: /timer <start|stop|time <seconds>|status>"
            );
        }
    }
}