using System;
using System.Linq;
using System.Xml.Serialization;

namespace CodeLocks.Locks
{
    [Serializable]
    public class AttemptsConfig
    {
        public float Cooldown { get; set; }

        [XmlIgnore]
        public byte[]? Damages
        {
            get => ParsedDamages?.Select(x => (byte) x).ToArray() ?? new byte[] {0, 30, 50, 255};
            set => ParsedDamages = value?.Select(x => (int)x).ToArray();
        }
        

        [XmlArray("Damages")]
        [XmlArrayItem("Damage")]
        public int[]? ParsedDamages { get; set; }

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
