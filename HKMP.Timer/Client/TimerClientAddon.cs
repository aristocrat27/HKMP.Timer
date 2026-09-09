using Hkmp.Api.Client;
using Hkmp.Api.Client.Networking;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using HkmpTimer;

namespace HKMP.Timer
{
    public class TimerClientAddon : ClientAddon
    {
        public new ILogger Logger => base.Logger;

        public override bool NeedsNetwork => true;

        protected override string Name => "HKMP.Timer";

        protected override string Version => "1.0.0.0";

        private IClientAddonNetworkSender<TimerServerPacketId> _sender;

        private TimerClientBehaviour _behaviour;

        public override void Initialize(IClientApi clientApi)
        {
            Logger.Info(
                "Initializing HKMP.Timer client addon"
            );

            _sender =
                clientApi.NetClient
                    .GetNetworkSender<TimerServerPacketId>(
                        this
                    );

            var receiver =
                clientApi.NetClient
                    .GetNetworkReceiver<TimerClientPacketId>(
                        this,
                        InstantiatePacket
                    );

            receiver.RegisterPacketHandler<TimerStatePacket>(
                TimerClientPacketId.TimerState,
                OnTimerState
            );

            receiver.RegisterPacketHandler<ClockSyncResponsePacket>(
                TimerClientPacketId.ClockSyncResponse,
                OnClockSyncResponse
            );

            _behaviour =
                TimerClientBehaviour.Create(this);

            Logger.Info(
                "HKMP.Timer client addon initialized."
            );
        }

        public void RequestClockSync()
        {
            if (_sender == null)
            {
                return;
            }

            try
            {
                _sender.SendSingleData(
                    TimerServerPacketId.ClockSyncRequest,
                    new ClockSyncRequestPacket
                    {
                        ClientSendUtcTicks =
                            System.DateTime.UtcNow.Ticks
                    }
                );
            }
            catch (System.Exception ex)
            {
                Logger.Debug(
                    "Clock sync request failed: " +
                    ex.Message
                );
            }
        }

        private void OnTimerState(
            TimerStatePacket packet
        )
        {
            if (_behaviour == null)
            {
                return;
            }

            _behaviour.ApplyTimerState(packet);
        }

        private void OnClockSyncResponse(
            ClockSyncResponsePacket packet
        )
        {
            if (_behaviour == null)
            {
                return;
            }

            _behaviour.ApplyClockSyncResponse(
                packet
            );
        }

        private static IPacketData InstantiatePacket(
            TimerClientPacketId packetId
        )
        {
            switch (packetId)
            {
                case TimerClientPacketId.TimerState:
                    return new TimerStatePacket();

                case TimerClientPacketId.ClockSyncResponse:
                    return new ClockSyncResponsePacket();

                default:
                    return null;
            }
        }
    }
}