using CodeLocks.Locks;
using Rocket.API;

namespace CodeLocks.Configuration
{
    public class CodeLocksConfiguration : IRocketPluginConfiguration
    {
        public EffectsConfig? Effects { get; set; } = new();

        public AttemptsConfig? Attempts { get; set; } = new();

		public void LoadDefaults()
        {
            Effects = new();

            Attempts = new();
        }
    }
}
