using ZenKit;

namespace GUZ.Core.Services.Context
{
    public class ContextGameVersionService : IContextGameVersionService
    {
        private IContextGameVersionService _impl;

        public void SetImpl(IContextGameVersionService proxy)
        {
            _impl = proxy;
        }

        public GameVersion Version => _impl.Version;
        public string RootPath => _impl.RootPath;
        public string CutsceneFileSuffix => _impl.CutsceneFileSuffix;
        public string InitialWorld => _impl.InitialWorld;
    }
}
