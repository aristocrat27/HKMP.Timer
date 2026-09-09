using Hkmp.Networking.Packet;

namespace HkmpTimer
{
    public sealed class TimerStatePacket : IPacketData
    {
        public bool Running { get; set; }

        public int DurationSeconds { get; set; }

        public long StartUtcTicks { get; set; }

        public long RemainingMilliseconds { get; set; }

        public long ServerUtcTicks { get; set; }

        public bool Expired { get; set; }

        public void WriteData(IPacket packet)
        {
            packet.Write(Running);
            packet.Write(DurationSeconds);
            packet.Write(StartUtcTicks);
            packet.Write(RemainingMilliseconds);
            packet.Write(ServerUtcTicks);
            packet.Write(Expired);
        }

        public void ReadData(IPacket packet)
        {
            Running = packet.ReadBool();
            DurationSeconds = packet.ReadInt();
            StartUtcTicks = packet.ReadLong();
            RemainingMilliseconds = packet.ReadLong();
            ServerUtcTicks = packet.ReadLong();
            Expired = packet.ReadBool();
        }

        public bool IsReliable => true;

        public bool DropReliableDataIfNewerExists => true;
    }

    public sealed class ClockSyncRequestPacket : IPacketData
    {
        public long ClientSendUtcTicks { get; set; }

        public void WriteData(IPacket packet)
        {
            packet.Write(ClientSendUtcTicks);
        }

        public void ReadData(IPacket packet)
        {
            ClientSendUtcTicks = packet.ReadLong();
        }

        public bool IsReliable => true;

        public bool DropReliableDataIfNewerExists => true;
    }

    public sealed class ClockSyncResponsePacket : IPacketData
    {
        public long ClientSendUtcTicks { get; set; }

        public long ServerUtcTicks { get; set; }

        public void WriteData(IPacket packet)
        {
            packet.Write(ClientSendUtcTicks);
            packet.Write(ServerUtcTicks);
        }

        public void ReadData(IPacket packet)
        {
            ClientSendUtcTicks = packet.ReadLong();
            ServerUtcTicks = packet.ReadLong();
        }

        public bool IsReliable => true;

        public bool DropReliableDataIfNewerExists => true;
    }
}