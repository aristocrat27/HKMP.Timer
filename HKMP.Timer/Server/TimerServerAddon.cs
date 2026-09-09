using Hkmp.Api.Server;
using Hkmp.Logging;
using HkmpTimer;

namespace HKMP.Timer
{
    /// <summary>
    /// Серверная часть HKMP.Timer.
    /// </summary>
    public sealed class TimerServerAddon : ServerAddon
    {
        public new ILogger Logger => base.Logger;

        public override bool NeedsNetwork => true;

        protected override string Name =>
            TimerIdentifier.Name;

        protected override string Version =>
            TimerIdentifier.Version;

        public override void Initialize(
            IServerApi serverApi)
        {
            TimerServerManager manager =
                new TimerServerManager(
                    this,
                    serverApi
                );

            manager.Initialize();
        }
    }
}