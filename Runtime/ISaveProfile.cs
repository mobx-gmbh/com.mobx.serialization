namespace MobX.Serialization
{
    public interface ISaveProfile
    {
        public void SaveFile<T>(string fileName, T value, StoreOptions options = default);

        public T LoadFile<T>(string fileName, StoreOptions options = default);

        /// <summary>
        ///     Store the file to the profile but don't save it yet persistent.
        ///     Calling Save on the profile will save the file.
        /// </summary>
        public void StoreFile<T>(string fileName, T value, StoreOptions options = default);

        // TODO: Make this TryLoad
        public void ResolveFile<T>(string fileName, ref T value, StoreOptions options = default);

        public void DeleteFile<T>(string fileName);

        public void Save();
    }
}