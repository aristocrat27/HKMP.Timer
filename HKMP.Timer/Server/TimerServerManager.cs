using Hkmp.Api.Server;
using Hkmp.Api.Server.Networking;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using HkmpTimer;
using System;
using UnityEngine;

namespace HKMP.Timer
{
    public sealed class TimerServerManager
    {
        private readonly TimerServerAddon _addon;
        private readonly IServerApi _serverApi;
        private readonly Hkmp.Logging.ILogger _logger;

        private readonly
            IServerAddonNetworkSender<TimerClientPacketId>
            _sender;

        private readonly
            IServerAddonNetworkReceiver<TimerServerPacketId>
            _receiver;

        private int _durationSeconds = 60;

        private long _remainingMilliseconds;

        private long _elapsedMilliseconds;

        private long _startUtcTicks;

        private bool _running;

        private bool _expired;

        private bool _stopwatchMode;

        private bool _roundControlActive;

        private uint _roundControlRoundId;

        private GameObject _tickGameObject;

        private static TimerServerManager _instance;

        private static Action<uint> _roundExpirationHandler;

        public static TimerServerManager Instance
        {
            get
            {
                return _instance;
            }
        }

        public TimerServerManager(
            TimerServerAddon addon,
            IServerApi serverApi)
        {
            _addon =
                addon;

            _serverApi =
                serverApi;

            _instance =
                this;

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
                new TimerCommand(
                    this
                )
            );

            _serverApi.ServerManager.PlayerConnectEvent +=
                OnPlayerConnect;

            _logger.Info(
                "HKMP.Timer server manager initialized."
            );

            _logger.Info(
                "HKMP.Timer command registered: /timer"
            );

            EnsureTickBehaviour();
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
                NormalizeExpired();

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
                    _stopwatchMode
                        ? "Stopwatch is already running."
                        : "Timer is already running."
                );

                return;
            }

            bool stopwatchMode =
                IsStopwatchEnabled();

            if (stopwatchMode)
            {
                _stopwatchMode =
                    true;

                _running =
                    true;

                _expired =
                    false;

                _elapsedMilliseconds =
                    0;

                _remainingMilliseconds =
                    0;

                _startUtcTicks =
                    DateTime.UtcNow.Ticks;

                BroadcastState();

                sendMessage?.Invoke(
                    "Stopwatch started."
                );

                _logger.Info(
                    "Stopwatch started."
                );

                return;
            }

            if (_durationSeconds <= 0)
            {
                sendMessage?.Invoke(
                    "Please set a duration greater than 0 first."
                );

                return;
            }

            _stopwatchMode =
                false;

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
                elapsedMilliseconds =
                    0;
            }

            if (elapsedMilliseconds >
                durationMilliseconds)
            {
                elapsedMilliseconds =
                    durationMilliseconds;
            }

            _startUtcTicks =
                DateTime.UtcNow.Ticks -
                elapsedMilliseconds *
                TimeSpan.TicksPerMillisecond;

            _running =
                true;

            _expired =
                false;

            BroadcastState();

            sendMessage?.Invoke(
                "Timer started: " +
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
                    _stopwatchMode
                        ? "Stopwatch is already stopped."
                        : "Timer is already stopped."
                );

                BroadcastState();

                return;
            }

            if (_stopwatchMode)
            {
                _elapsedMilliseconds =
                    GetElapsedMilliseconds();
            }
            else
            {
                _remainingMilliseconds =
                    CalculateRemainingMilliseconds();
            }

            _running =
                false;

            _startUtcTicks =
                0;

            BroadcastState();

            sendMessage?.Invoke(
                _stopwatchMode
                    ? "Stopwatch stopped at " +
                      FormatMilliseconds(
                          _elapsedMilliseconds
                      )
                    : "Timer stopped at " +
                      FormatMilliseconds(
                          _remainingMilliseconds
                      )
            );

            _logger.Info(
                _stopwatchMode
                    ? "Stopwatch stopped."
                    : "Timer stopped."
            );
        }

        public void SetDuration(
            int seconds,
            Action<string> sendMessage = null)
        {
            NormalizeExpired();

            if ((_running &&
                 _stopwatchMode) ||
                IsStopwatchEnabled())
            {
                sendMessage?.Invoke(
                    "Setting duration is unavailable while in stopwatch mode."
                );

                return;
            }

            if (seconds < 0)
            {
                sendMessage?.Invoke(
                    "Duration cannot be negative."
                );

                return;
            }

            if (_roundControlActive)
            {
                sendMessage?.Invoke(
                    "Duration is controlled by HKMP.Rounds integration during active matches."
                );

                return;
            }

            _durationSeconds =
                seconds;

            _remainingMilliseconds =
                (long)seconds *
                1000L;

            _elapsedMilliseconds =
                0;

            _stopwatchMode =
                false;

            _expired =
                false;

            if (_running &&
                seconds > 0)
            {
                _startUtcTicks =
                    DateTime.UtcNow.Ticks;
            }
            else
            {
                _running =
                    false;

                _startUtcTicks =
                    0;
            }

            BroadcastState();

            sendMessage?.Invoke(
                "Timer duration changed to " +
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

            if (_stopwatchMode)
            {
                long elapsed =
                    GetElapsedMilliseconds();

                return
                    "Stopwatch: " +
                    FormatMilliseconds(
                        elapsed
                    ) +
                    (
                        _running
                            ? " (running)"
                            : " (stopped)"
                    );
            }

            long remaining =
                GetRemainingMilliseconds();

            if (_expired)
            {
                return
                    "Timer: TIME IS UP";
            }

            if (_running)
            {
                return
                    "Timer: " +
                    FormatMilliseconds(
                        remaining
                    ) +
                    " (running)";
            }

            return
                "Timer: " +
                FormatMilliseconds(
                    remaining
                ) +
                " (stopped)";
        }

        public static void SetRoundExpirationHandler(
            Action<uint> handler)
        {
            _roundExpirationHandler =
                handler;
        }

        public bool StartForRound(
            uint roundId)
        {
            if (roundId == 0 ||
                _running)
            {
                return false;
            }

            bool stopwatchMode =
                IsStopwatchEnabled();

            if (!stopwatchMode &&
                _durationSeconds <= 0)
            {
                return false;
            }

            _stopwatchMode =
                stopwatchMode;

            _running =
                true;

            _expired =
                false;

            _roundControlActive =
                true;

            _roundControlRoundId =
                roundId;

            _startUtcTicks =
                DateTime.UtcNow.Ticks;

            _elapsedMilliseconds =
                0;

            _remainingMilliseconds =
                stopwatchMode
                    ? 0L
                    : (long)_durationSeconds *
                      1000L;

            BroadcastState();

            _logger.Info(
                stopwatchMode
                    ? "Round stopwatch started for round " +
                      roundId +
                      "."
                    : "Round timer started for round " +
                      roundId +
                      " with duration " +
                      _durationSeconds +
                      " seconds."
            );

            return true;
        }

        public bool EndForRound(
            uint roundId)
        {
            if (!_roundControlActive ||
                _roundControlRoundId != roundId)
            {
                return false;
            }

            if (_stopwatchMode &&
                _running)
            {
                _elapsedMilliseconds =
                    GetElapsedMilliseconds();
            }
            else if (!_stopwatchMode &&
                     _running)
            {
                _remainingMilliseconds =
                    CalculateRemainingMilliseconds();
            }

            _running =
                false;

            _startUtcTicks =
                0;

            _roundControlActive =
                false;

            _roundControlRoundId =
                0;

            BroadcastState();

            if (_stopwatchMode)
            {
                _serverApi.ServerManager.BroadcastMessage(
                    "Round duration: " +
                    FormatMilliseconds(
                        _elapsedMilliseconds
                    )
                );

                _logger.Info(
                    "Round stopwatch ended for round " +
                    roundId +
                    ": " +
                    FormatMilliseconds(
                        _elapsedMilliseconds
                    ) +
                    "."
                );
            }
            else
            {
                _logger.Info(
                    "Round timer stopped for round " +
                    roundId +
                    "."
                );
            }

            return true;
        }

        private long GetRemainingMilliseconds()
        {
            if (!_running ||
                _stopwatchMode)
            {
                return Math.Max(
                    0L,
                    _remainingMilliseconds
                );
            }

            return CalculateRemainingMilliseconds();
        }

        private long GetElapsedMilliseconds()
        {
            if (!_stopwatchMode)
            {
                return 0L;
            }

            if (!_running)
            {
                return Math.Max(
                    0L,
                    _elapsedMilliseconds
                );
            }

            long elapsedTicks =
                DateTime.UtcNow.Ticks -
                _startUtcTicks;

            if (elapsedTicks <= 0)
            {
                return 0L;
            }

            return
                elapsedTicks /
                TimeSpan.TicksPerMillisecond;
        }

        private long CalculateRemainingMilliseconds()
        {
            if (!_running ||
                _stopwatchMode)
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

        private bool IsStopwatchEnabled()
        {
            return
                TimerMod.GlobalSettings != null &&
                TimerMod.GlobalSettings.StopwatchEnabled;
        }

        private void Tick()
        {
            if (!_running ||
                _stopwatchMode)
            {
                return;
            }

            long remaining =
                CalculateRemainingMilliseconds();

            _remainingMilliseconds =
                remaining;

            if (remaining > 0)
            {
                return;
            }

            _running =
                false;

            _remainingMilliseconds =
                0;

            _startUtcTicks =
                0;

            _expired =
                true;

            bool roundControlled =
                _roundControlActive;

            uint roundId =
                _roundControlRoundId;

            _roundControlActive =
                false;

            _roundControlRoundId =
                0;

            BroadcastState();

            _logger.Info(
                roundControlled
                    ? "Round timer expired for round " +
                      roundId +
                      "."
                    : "Timer reached zero."
            );

            if (roundControlled)
            {
                Action<uint> handler =
                    _roundExpirationHandler;

                if (handler != null)
                {
                    try
                    {
                        handler(
                            roundId
                        );
                    }
                    catch (Exception exception)
                    {
                        _logger.Warn(
                            "Round expiration handler failed: " +
                            exception.Message
                        );
                    }
                }

                return;
            }

            _serverApi.ServerManager.BroadcastMessage(
                "Time's up."
            );
        }

        private void NormalizeExpired()
        {
            if (!_running ||
                _stopwatchMode)
            {
                return;
            }

            long remaining =
                CalculateRemainingMilliseconds();

            if (remaining <= 0)
            {
                Tick();
                return;
            }

            _remainingMilliseconds =
                remaining;
        }

        private TimerStatePacket CreateStatePacket()
        {
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
                    _expired,

                StopwatchMode =
                    _stopwatchMode,

                ElapsedMilliseconds =
                    GetElapsedMilliseconds()
            };
        }

        private void BroadcastState()
        {
            TimerStatePacket packet =
                CreateStatePacket();

            foreach (
                IServerPlayer player
                in _serverApi.ServerManager.Players)
            {
                if (player == null)
                {
                    continue;
                }

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

        private void EnsureTickBehaviour()
        {
            if (_tickGameObject != null)
            {
                return;
            }

            GameObject gameObject =
                new GameObject(
                    "HKMP.Timer.ServerTick"
                );

            UnityEngine.Object.DontDestroyOnLoad(
            gameObject
            );

            _tickGameObject =
                gameObject;

            ServerTickBehaviour behaviour =
                gameObject.AddComponent<
                    ServerTickBehaviour
                >();

            behaviour.Initialize(
                this
            );
        }

        private sealed class ServerTickBehaviour :
            MonoBehaviour
        {
            private TimerServerManager _manager;

            public void Initialize(
                TimerServerManager manager)
            {
                _manager =
                    manager;
            }

            private void Update()
            {
                if (_manager == null)
                {
                    return;
                }

                _manager.Tick();
            }

            private void OnDestroy()
            {
                if (
                    _manager != null &&
                    _manager._tickGameObject ==
                    gameObject
                )
                {
                    _manager._tickGameObject =
                        null;
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


