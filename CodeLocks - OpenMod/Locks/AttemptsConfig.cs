using System;

namespace CodeLocks.Locks
{
    [Serializable]
    public class AttemptsConfig
    {
        public float Cooldown { get; set; }

        public byte[]? Damages { get; set; }

        public AttemptsConfig()
        {
            Cooldown = 60;
            Damages = new byte[]
            {
                0, 30, 50, 255
            };
        }
    }
}
