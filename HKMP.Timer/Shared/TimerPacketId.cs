namespace HKMP.Timer
{
    /// <summary>
    /// Пакеты, которые клиент отправляет серверу таймера.
    /// </summary>
    public enum TimerServerPacketId
    {
        ClockSyncRequest
    }

    /// <summary>
    /// Пакеты, которые сервер таймера отправляет клиенту.
    /// </summary>
    public enum TimerClientPacketId
    {
        TimerState,
        ClockSyncResponse
    }
}