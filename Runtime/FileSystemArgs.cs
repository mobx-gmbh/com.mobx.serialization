using JetBrains.Annotations;
using MobX.Utilities.Types;
using System;
using UnityEngine;

namespace MobX.Serialization
{
    [Serializable]
    public struct FileSystemArgs
    {
        [Header("General")]
        [Tooltip("The root folder fot the files system (relative to the application data path).")]
        public string rootFolder;

        [Tooltip("When enabled, root folder are versioned.")]
        public bool versionRootFolder;

        [Tooltip("When enabled, the unity version will be used instead of the version string defined below")]
        public bool useUnityVersion;

        [Tooltip("The root folder fot the files system (relative to the application data path).")]
        public string version;

        [Header("File Endings")]
        [Tooltip("Custom file ending that is used for files without specifically set file endings.")]
        public string fileEnding;

        [Tooltip("Array to limit the use of file endings. If empty, every file ending can be used.")]
        public Optional<string[]> enforceFileEndings;

        [Tooltip("When enabled, the initialization process is forced to execute synchronous.")]
        public bool forceSynchronous;

        [Header("Profiles")]
        [Tooltip("The default name used for created profiles.")]
        public string defaultProfileName;

        [Tooltip("Limits the amount of available profiles. 0 to set no limit.")]
        public Optional<uint> profileLimit;

        [Header("Conversion")]
        [Tooltip("Save game converter objects can use custom logic to convert old saves to be file system compatible.")]
        public Optional<SaveGameConverter> saveGameConverter;

        [Header("Logging")]
        [Tooltip("When enabled, exceptions are logged to the console.")]
        public LoggingLevel exceptions;

        [Tooltip("When enabled, a warning is logged when a file name is passed without a specified file extension.")]
        public bool logMissingFileExtensionWarning;

        [Header("Encryption")]
        [Tooltip("Custom encryption provider.")]
        public Optional<EncryptionProviderAsset> encryptionAsset;

        [Tooltip("Custom encryption provider index.")]
        public EncryptionProviderAsset[] encryptionProvider;

        [Tooltip("Custom encryption pass phrase. If none is provided a default value is used.")]
        public Optional<string> encryptionKey;

        [Tooltip("Custom encryption provider.")] [UsedImplicitly]
        public IEncryptionProvider EncryptionProvider;

        [Tooltip("Custom platform file storage providers")]
        public Optional<FileOperationAsset> fileStorageProvider;
    }
}