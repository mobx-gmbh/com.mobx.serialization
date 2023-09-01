using System;

namespace MobX.Serialization
{
    [Serializable]
    public struct Storage<T>
    {
        public T value;

        public Storage(T value)
        {
            this.value = value;
        }
    }
}
