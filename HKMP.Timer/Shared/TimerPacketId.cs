namespace HKMP.Timer
{
    public enum TimerServerPacketId
    {
        ClockSyncRequest,
        TimerActionRequest
    }
    public enum TimerClientPacketId
    {
        TimerState,
        ClockSyncResponse
    }
}
