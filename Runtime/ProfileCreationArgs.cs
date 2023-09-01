using System;

namespace MobX.Serialization
{
    [Serializable]
    public struct ProfileCreationArgs
    {
        public string name;
        public bool activate;
    }
}