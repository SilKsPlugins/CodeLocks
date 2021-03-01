using System;

namespace CodeLocks.Configuration
{
    [Serializable]
    public class EffectsConfig
    {
        public ushort UI { get; set; } = 29123;
        
        public ushort Success { get; set; } = 29124;

        public ushort Failure { get; set; } = 29125;
    }
}
