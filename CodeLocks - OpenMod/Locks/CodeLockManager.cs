using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CodeLocks.Locks
{
    public class CodeLockManager
    {
        private readonly ObjectManager _objectManager;

        public CodeLockManager(ObjectManager objectManager)
        {
            _objectManager = objectManager;
        }

        private string _workingDirectory = "";
        private readonly Dictionary<uint, CodeLockInfo> _codeLocks = new Dictionary<uint, CodeLockInfo>();

        public string GetCodeLockFile() => Path.Combine(_workingDirectory, "codelocks.dat");
        
        public async Task LoadAsync(string workingDirectory)
        {
            _workingDirectory = workingDirectory;

            if (string.IsNullOrWhiteSpace(_workingDirectory)) return;

            var file = GetCodeLockFile();

            if (!File.Exists(file)) return;

            var lines = File.ReadAllLines(file);

            _codeLocks.Clear();
            
            using var reader = new StreamReader(file);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (line == null) continue;

                if (CodeLockInfo.TryParse(line, out var lockInfo))
                    _codeLocks.Add(lockInfo.InstanceId, lockInfo);
            }
        }

        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(_workingDirectory)) return;

            var file = GetCodeLockFile();

            using var writer = new StreamWriter(file);

            foreach (var pair in _codeLocks)
            {
                await writer.WriteLineAsync(pair.Value.ToString());
            }
        }

        public void AddCodeLock(uint instanceId, ushort code, ulong owner)
        {
            if (GetCodeLock(instanceId) != null)
                throw new Exception($"Code lock already exists for an object with the instance id {instanceId}.");

            var codeLock = new CodeLockInfo(instanceId, code, new List<ulong> {owner});

            _codeLocks.Add(codeLock.InstanceId, codeLock);

            _objectManager.ChangeOwnerAndGroup(codeLock.InstanceId, owner, 0);

            _objectManager.UpdateBarricade(codeLock);
        }

        public void RemoveCodeLock(uint instanceId)
        {
            if (GetCodeLock(instanceId) == null)
                throw new Exception($"No code lock exists for an object with the instance id {instanceId}.");

            _codeLocks.Remove(instanceId);
        }

        public CodeLockInfo? GetCodeLock(uint instanceId) => _codeLocks.TryGetValue(instanceId, out var codeLock) ? codeLock : null;

        public IReadOnlyCollection<CodeLockInfo> GetAllCodeLocks() => _codeLocks.Values.ToList().AsReadOnly();
    }
}
