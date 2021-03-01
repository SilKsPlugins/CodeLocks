using Steamworks;

namespace CodeLocks.Locks
{
    public class Attempt
    {
        public CSteamID SteamId { get; set; }

        public uint InstanceId { get; set; }

        public float Time { get; set; }

        public Attempt(CSteamID steamId, uint instanceId)
        {
            SteamId = steamId;
            InstanceId = instanceId;
            Time = UnityEngine.Time.time;
        }
    }
}
