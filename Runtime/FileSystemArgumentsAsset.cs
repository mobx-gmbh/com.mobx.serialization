using Cysharp.Threading.Tasks;
using MobX.Inspector;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Serialization
{
    public class FileSystemArgumentsAsset : ScriptableObject
    {
        [Foldout("Arguments")]
        [SerializeField] private FileSystemArgs args;
        public FileSystemArgs Args => args;

        [ReadOnly]
        [Foldout("Controls")]
        private FileSystemState State => FileSystem.State;

        [Button]
        [PropertySpace]
        [Foldout("Controls")]
        public UniTask Initialize()
        {
            return FileSystem.InitializeAsync(args);
        }

        [Button]
        [Foldout("Controls")]
        public void Shutdown()
        {
            FileSystem.Shutdown();
        }

        [Button]
        [PropertySpace]
        [Foldout("Controls")]
        public void OpenDataPath()
        {
            Application.OpenURL(Application.persistentDataPath);
        }
    }
}