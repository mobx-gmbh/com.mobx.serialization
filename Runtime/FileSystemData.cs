using System;

namespace MobX.Serialization
{
    [Serializable]
    internal struct FileSystemData
    {
        public string activeProfileFilePath;
        public int nextProfileIndex;
    }
}