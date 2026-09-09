using Hkmp.Api.Server;
using Hkmp.Api.Server.Networking;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using HkmpTimer;
using System;

namespace HKMP.Timer
{
    public sealed class TimerServerManager
    {
        private readonly TimerServerAddon _addon;
        private readonly IServerApi _serverApi;
        private readonly ILogger _logger;

        private readonly
            IServerAddonNetworkSender<TimerClientPacketId>
            _sender;

        private readonly
            IServerAddonNetworkReceiver<TimerServerPacketId>
            _receiver;

        private int _durationSeconds = 60;

        private long _remainingMilliseconds;

        private long _startUtcTicks;

        private bool _running;

        private bool _expired;

        public TimerServerManager(
            TimerServerAddon addon,
            IServerApi serverApi)
        {
            _addon = addon;

            _serverApi = serverApi;

            _logger =
                addon.Logger;

            _sender =
                serverApi.NetServer
                    .GetNetworkSender<
                        TimerClientPacketId
                    >(
                        addon
                    );

            _receiver =
                serverApi.NetServer
                    .GetNetworkReceiver<
                        TimerServerPacketId
                    >(
                        addon,
                        InstantiatePacket
                    );
        }

        public void Initialize()
        {
            _receiver.RegisterPacketHandler<
                ClockSyncRequestPacket
            >(
                TimerServerPacketId.ClockSyncRequest,
                OnClockSyncRequest
            );

            _serverApi.CommandManager.RegisterCommand(
                new TimerCommand(this)
            );

            _serverApi.ServerManager.PlayerConnectEvent +=
                OnPlayerConnect;

            _logger.Info(
                "HKMP.Timer server manager initialized."
            );

            _logger.Info(
                "HKMP.Timer command registered: /timer"
            );
        }

        private void OnPlayerConnect(
            IServerPlayer player)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                SendStateToPlayer(
                    player.Id
                );

                _logger.Info(
                    "Sent current timer state to player " +
                    player.Id +
                    "."
                );
            }
            catch (Exception ex)
            {
                _logger.Warn(
                    "Could not send timer state to newly connected player " +
                    player.Id +
                    ": " +
                    ex.Message
                );
            }
        }

        private void SendStateToPlayer(
            ushort playerId)
        {
            TimerStatePacket packet =
                CreateStatePacket();

            _sender.SendSingleData(
                TimerClientPacketId.TimerState,
                packet,
                playerId
            );
        }

        private void OnClockSyncRequest(
            ushort playerId,
            ClockSyncRequestPacket packet)
        {
            if (packet == null)
            {
                return;
            }

            _sender.SendSingleData(
                TimerClientPacketId.ClockSyncResponse,
                new ClockSyncResponsePacket
                {
                    ClientSendUtcTicks =
                        packet.ClientSendUtcTicks,

                    ServerUtcTicks =
                        DateTime.UtcNow.Ticks
                },
                playerId
            );
        }

        public void Start(
            Action<string> sendMessage = null)
        {
            NormalizeExpired();

            if (_running)
            {
                sendMessage?.Invoke(
                    "Таймер уже запущен."
                );

                return;
            }

            if (_durationSeconds <= 0)
            {
                sendMessage?.Invoke(
                    "Сначала задайте время больше 0."
                );

                return;
            }

            if (_remainingMilliseconds <= 0)
            {
                _remainingMilliseconds =
                    (long)_durationSeconds *
                    1000L;
            }

            long durationMilliseconds =
                (long)_durationSeconds *
                1000L;

            long elapsedMilliseconds =
                durationMilliseconds -
                _remainingMilliseconds;

            if (elapsedMilliseconds < 0)
            {
                elapsedMilliseconds = 0;
            }

            if (
                elapsedMilliseconds >
                durationMilliseconds
            )
            {
                elapsedMilliseconds =
                    durationMilliseconds;
            }

            _startUtcTicks =
                DateTime.UtcNow.Ticks -
                elapsedMilliseconds *
                TimeSpan.TicksPerMillisecond;

            _running = true;

            _expired = false;

            BroadcastState();

            sendMessage?.Invoke(
                "Таймер запущен: " +
                FormatMilliseconds(
                    _remainingMilliseconds
                )
            );

            _logger.Info(
                "Timer started."
            );
        }

        public void Stop(
            Action<string> sendMessage = null)
        {
            NormalizeExpired();

            if (!_running)
            {
                sendMessage?.Invoke(
                    "Таймер уже остановлен."
                );

                BroadcastState();

                return;
            }

            _remainingMilliseconds =
                CalculateRemainingMilliseconds();

            _running = false;

            _startUtcTicks = 0;

            BroadcastState();

            sendMessage?.Invoke(
                "Таймер остановлен на " +
                FormatMilliseconds(
                    _remainingMilliseconds
                )
            );

            _logger.Info(
                "Timer stopped."
            );
        }

        public void SetDuration(
            int seconds,
            Action<string> sendMessage = null)
        {
            NormalizeExpired();

            if (seconds < 0)
            {
                sendMessage?.Invoke(
                    "Время не может быть отрицательным."
                );

                return;
            }

            _durationSeconds =
                seconds;

            _remainingMilliseconds =
                (long)seconds *
                1000L;

            _expired = false;

            if (
                _running &&
                seconds > 0
            )
            {
                _startUtcTicks =
                    DateTime.UtcNow.Ticks;
            }
            else
            {
                _running = false;

                _startUtcTicks = 0;
            }

            BroadcastState();

            sendMessage?.Invoke(
                "Таймер установлен на " +
                FormatMilliseconds(
                    _remainingMilliseconds
                )
            );

            _logger.Info(
                "Timer duration changed to " +
                seconds +
                " seconds."
            );
        }

        public string GetStatusMessage()
        {
            NormalizeExpired();

            long remaining =
                GetRemainingMilliseconds();

            if (_expired)
            {
                return
                    "Таймер: ВРЕМЯ ВЫШЛО";
            }

            if (_running)
            {
                return
                    "Таймер: " +
                    FormatMilliseconds(
                        remaining
                    ) +
                    " (запущен)";
            }

            return
                "Таймер: " +
                FormatMilliseconds(
                    remaining
                ) +
                " (остановлен)";
        }

        private long GetRemainingMilliseconds()
        {
            if (!_running)
            {
                return Math.Max(
                    0L,
                    _remainingMilliseconds
                );
            }

            return CalculateRemainingMilliseconds();
        }

        private long CalculateRemainingMilliseconds()
        {
            if (!_running)
            {
                return Math.Max(
                    0L,
                    _remainingMilliseconds
                );
            }

            long endTicks =
                _startUtcTicks +
                (long)_durationSeconds *
                TimeSpan.TicksPerSecond;

            long remainingTicks =
                endTicks -
                DateTime.UtcNow.Ticks;

            if (remainingTicks <= 0)
            {
                return 0;
            }

            return
                remainingTicks /
                TimeSpan.TicksPerMillisecond;
        }

        private void NormalizeExpired()
        {
            if (!_running)
            {
                return;
            }

            long remaining =
                CalculateRemainingMilliseconds();

            if (remaining <= 0)
            {
                _running = false;

                _remainingMilliseconds = 0;

                _startUtcTicks = 0;

                _expired = true;

                BroadcastState();

                _logger.Info(
                    "Timer reached zero."
                );

                return;
            }

            _remainingMilliseconds =
                remaining;
        }

        private TimerStatePacket CreateStatePacket()
        {
            if (_running)
            {
                NormalizeExpired();
            }

            return new TimerStatePacket
            {
                Running =
                    _running,

                DurationSeconds =
                    _durationSeconds,

                StartUtcTicks =
                    _startUtcTicks,

                RemainingMilliseconds =
                    GetRemainingMilliseconds(),

                ServerUtcTicks =
                    DateTime.UtcNow.Ticks,

                Expired =
                    _expired
            };
        }

        private void BroadcastState()
        {
            TimerStatePacket packet =
                CreateStatePacket();

            foreach (
                var player
                in _serverApi.ServerManager.Players
            )
            {
                try
                {
                    _sender.SendSingleData(
                        TimerClientPacketId.TimerState,
                        packet,
                        player.Id
                    );
                }
                catch (Exception ex)
                {
                    _logger.Warn(
                        "Could not send timer state to player " +
                        player.Id +
                        ": " +
                        ex.Message
                    );
                }
            }
        }

        private static IPacketData InstantiatePacket(
            TimerServerPacketId packetId)
        {
            switch (packetId)
            {
                case TimerServerPacketId.ClockSyncRequest:
                    return new ClockSyncRequestPacket();

                default:
                    return null;
            }
        }

        private static string FormatMilliseconds(
            long milliseconds)
        {
            milliseconds =
                Math.Max(
                    0L,
                    milliseconds
                );

            long totalSeconds =
                milliseconds / 1000L;

            long hours =
                totalSeconds / 3600L;

            long minutes =
                (totalSeconds % 3600L) /
                60L;

            long seconds =
                totalSeconds % 60L;

            if (hours > 0)
            {
                return string.Format(
                    "{0:00}:{1:00}:{2:00}",
                    hours,
                    minutes,
                    seconds
                );
            }

            return string.Format(
                "{0:00}:{1:00}",
                minutes,
                seconds
            );
        }
    }
}