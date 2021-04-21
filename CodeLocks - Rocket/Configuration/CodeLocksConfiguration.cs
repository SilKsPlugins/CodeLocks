using CodeLocks.Locks;
using Rocket.API;

namespace CodeLocks.Configuration
{
    public class CodeLocksConfiguration : IRocketPluginConfiguration
    {
        public EffectsConfig? Effects { get; set; } = new();

        public bool RememberOwner;
        public bool RememberUsers;
        public bool NonOwnerCanChangeCode;

        public AttemptsConfig? Attempts { get; set; } = new();

		public void LoadDefaults()
        {
            Effects = new();

            RememberOwner = true;
            RememberUsers = true;
            NonOwnerCanChangeCode = true;

            Attempts = new();
        }
    }
}
