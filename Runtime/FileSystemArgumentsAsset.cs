using Cysharp.Threading.Tasks;
using MobX.Utilities.Inspector;
using UnityEngine;

namespace MobX.Serialization
{
    public class FileSystemArgumentsAsset : ScriptableObject
    {
        [Foldout("Arguments")]
        [SerializeField] private FileSystemArgs args;
        public FileSystemArgs Args => args;

        [ReadonlyInspector]
        [Foldout("Controls")]
        private FileSystemState State => FileSystem.State;

        [Button]
        [SpaceBefore]
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
        [SpaceBefore]
        [Foldout("Controls")]
        public void OpenDataPath()
        {
            Application.OpenURL(Application.persistentDataPath);
        }
    }
}