using CodeLocks.Locks;
using Rocket.API;

namespace CodeLocks.Configuration
{
    public class CodeLocksConfiguration : IRocketPluginConfiguration
    {
        public EffectsConfig? Effects { get; set; } = new();

        public bool RememberOwner { get; set; }
        public bool RememberUsers { get; set; }
        public bool NonOwnerCanChangeCode { get; set; }
        public bool OnlyOwnerCanLock { get; set; }

        public AttemptsConfig? Attempts { get; set; } = new();

		public void LoadDefaults()
        {
            Effects = new();

            RememberOwner = true;
            RememberUsers = true;
            NonOwnerCanChangeCode = true;
            OnlyOwnerCanLock = false;

            Attempts = new();
        }
    }
}
