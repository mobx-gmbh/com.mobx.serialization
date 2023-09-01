namespace MobX.Serialization
{
    public readonly ref struct FileStorageArguments
    {
        public readonly bool ForceSynchronous;
        public readonly string RootFolder;
        public readonly string EncryptionKey;
        public readonly LoggingLevel ExceptionLogging;
        public readonly IEncryptionProvider EncryptionProvider;
        public readonly IEncryptionProvider[] EncryptionProviders;
        public readonly IFileOperations FileOperations;

        public FileStorageArguments(
            string rootFolder,
            string encryptionKey,
            IEncryptionProvider encryptionProvider,
            LoggingLevel exceptionLogging,
            IEncryptionProvider[] encryptionProviders,
            bool forceSynchronous,
            IFileOperations fileOperations)
        {
            RootFolder = rootFolder;
            EncryptionKey = encryptionKey;
            ExceptionLogging = exceptionLogging;
            EncryptionProvider = encryptionProvider;
            EncryptionProviders = encryptionProviders;
            ForceSynchronous = forceSynchronous;
            FileOperations = fileOperations;
        }
    }
}
