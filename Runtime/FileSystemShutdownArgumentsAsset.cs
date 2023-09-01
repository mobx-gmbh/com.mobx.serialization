using MobX.Utilities.Inspector;
using UnityEngine;

namespace MobX.Serialization
{
    public class FileSystemShutdownArgumentsAsset : ScriptableObject
    {
        [SerializeField] private FileSystemShutdownArgs args;
        public FileSystemShutdownArgs Args => args;

        [Button]
        public void Shutdown()
        {
            FileSystem.Shutdown(args);
        }
    }
}