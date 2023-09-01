using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MobX.Serialization
{
    public abstract class SaveGameConverter : ScriptableObject
    {
        [SerializeField] private FileSystemArgs converterFileSystem;
        public FileSystemArgs FileSystemArgs => converterFileSystem;

        public abstract UniTask ConvertAsync(IFileStorage storage, IProfile profile, IProfile systemProfile);

        public abstract void Convert(IFileStorage storage, IProfile profile, IProfile systemProfile);
    }
}
