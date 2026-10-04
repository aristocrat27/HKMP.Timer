namespace HKMP.Timer
{
    public enum TimerServerPacketId
    {
        ClockSyncRequest
    }
    public enum TimerClientPacketId
    {
        TimerState,
        ClockSyncResponse
    }
}
