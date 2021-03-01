using System;
using System.Collections.Generic;

namespace CodeLocks.Locks
{
    [Serializable]
    public class CodeLockInfo
    {
        public uint InstanceId { get; set; }

        public ushort Code { get; set; }

        public List<ulong> Users { get; set; }

        public CodeLockInfo()
        {
            InstanceId = 0;
            Code = 0;
            Users = new List<ulong>();
        }

        public CodeLockInfo(uint instanceId, ushort code, List<ulong> users)
        {
            InstanceId = instanceId;
            Code = code;
            Users = users;
        }

        public static ushort ParseCode(string code)
        {
            code = code.TrimStart('0');

            return code == "" ? (ushort)0 : ushort.Parse(code);
        }

        public static bool TryParse(string s, out CodeLockInfo lockInfo)
        {
            lockInfo = null!;

            if (string.IsNullOrWhiteSpace(s)) return false;

            var parts = s.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2 && parts.Length != 3) return false;

            if (!uint.TryParse(parts[0], out var instanceId)) return false;

            if (!ushort.TryParse(parts[1], out var code)) return false;

            var users = new List<ulong>();

            if (parts.Length == 3)
            {
                foreach (var userStr in parts[2].Split(','))
                {
                    if (ulong.TryParse(userStr, out var steamId))
                        users.Add(steamId);
                }
            }

            lockInfo = new CodeLockInfo(instanceId, code, users);

            return true;
        }

        public override string ToString() => $"{InstanceId} {Code} {string.Join(",", Users)}";
    }
}
