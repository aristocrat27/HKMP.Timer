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
                    "Использование: /timer time <секунды>"
                );

                return;
            }

            int seconds;

            if (!int.TryParse(
                    args[2],
                    out seconds))
            {
                commandSender.SendMessage(
                    "Ошибка: время должно быть целым числом секунд."
                );

                return;
            }

            if (seconds < 0)
            {
                commandSender.SendMessage(
                    "Ошибка: время не может быть отрицательным."
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
                "Использование: /timer <start|stop|time <секунды>|status>"
            );
        }
    }
}