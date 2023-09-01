using Cysharp.Threading.Tasks;
using MobX.Utilities;
using MobX.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace MobX.Serialization
{
    [Serializable]
    internal class Profile : IProfile
    {
        #region Fields & Properties

        public bool IsLoaded { get; private set; }

        public string HeaderFilePath => Path.Combine(folderName, headerName);

        public string DisplayName => displayName;

        public string FolderName => folderName;

        public string Version => version;

        public DateTime Created =>
            _created ??= DateTime.TryParse(createdTimeStamp, out var dateTime) ? dateTime : default(DateTime);

        public DateTime Saved =>
            _saved ??= DateTime.TryParse(lastSaveTimeStamp, out var dateTime) ? dateTime : default(DateTime);
        public IReadOnlyCollection<FileHeader> FileHeaders => headerFiles.Values;

        #endregion


        #region Fields

        private Dictionary<string, object> _fileBuffer;
        private Dictionary<string, FileData> _dataBuffer;
        private HashSet<FileHeader> _dirtyFiles = new();

        private DateTime? _created;
        private DateTime? _saved;

        #endregion


        #region Serialized

        [SerializeField] private string displayName;
        [SerializeField] private string folderName;
        [SerializeField] private string headerName;
        [SerializeField] private string createdTimeStamp;
        [SerializeField] private string lastSaveTimeStamp;
        [SerializeField] private string version;
        [SerializeField] private Map<string, FileHeader> headerFiles;

        #endregion


        #region File Header

        public bool TryGetHeader(string fileName, out ReadonlyFileHeader header)
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (headerFiles.TryGetValue(fileName, out var headerFile))
            {
                header = new ReadonlyFileHeader(headerFile);
            }

            header = default(ReadonlyFileHeader);
            return false;
        }

        private void UpdateHeader<T>(string fileName, StoreOptions options)
        {
            if (headerFiles.TryGetValue(fileName, out var headerFile))
            {
                headerFile.Update(options);
                headerFile.FileName = fileName;
                headerFile.FileType = typeof(T).AssemblyQualifiedName;
            }
            else
            {
                headerFile = new FileHeader(fileName, typeof(T), options);
                headerFiles.Add(fileName, headerFile);
            }

            _dirtyFiles.Add(headerFile);
        }

        #endregion


        #region Store

        public void Store<T>(string fileName, T file, StoreOptions options = default)
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            _fileBuffer.Update(fileName, file);
            UpdateHeader<T>(fileName, options);
        }

        public void StoreFile<T>(string fileName, T file, StoreOptions options = default) where T : class
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            _fileBuffer.Update(fileName, file);
            UpdateHeader<T>(fileName, options);
        }

        public void StoreData<T>(string fileName, T data, StoreOptions options = default) where T : struct
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            _fileBuffer.Update(fileName, data);

            UpdateHeader<T>(fileName, options);
        }

        public void StoreAsset<T>(string fileName, T asset, StoreOptions options = default) where T : ScriptableObject
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            var data = Json.ToJson(asset);

            var fileData = FileData.FromSuccess(data);

            _dataBuffer.Update(fileName, fileData);

            UpdateHeader<T>(fileName, options);

#if UNITY_EDITOR
            FileSystem.Validator.ValidateScriptableObjectData(fileData.RawData, asset);
#endif
        }

        #endregion


        #region Initialize File

        public void Resolve<T>(string fileName, out T file, StoreOptions options = default) where T : new()
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_fileBuffer.TryGetValue(fileName, out var result))
            {
                file = result.TryCast<T>();
                return;
            }

            if (!_dataBuffer.TryGetValue(fileName, out var data) || !data.IsValid)
            {
                file = new T();
                Store(fileName, file, options);
                return;
            }

            file = data.Read<T>();
            _fileBuffer.Update(fileName, data.Read<T>());
            _dataBuffer.Remove(fileName);
        }

        public void ResolveFile<T>(string fileName, out T file, StoreOptions options = default)
            where T : class, new()
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_fileBuffer.TryGetValue(fileName, out var result))
            {
                file = result.TryCast<T>();
                return;
            }

            if (!_dataBuffer.TryGetValue(fileName, out var data) || !data.IsValid)
            {
                file = new T();
                StoreFile(fileName, file, options);
                return;
            }

            file = data.Read<T>();
            _fileBuffer.Update(fileName, data.Read<T>());
            _dataBuffer.Remove(fileName);
        }

        public void ResolveData<T>(string fileName, out T file, StoreOptions options = default) where T : struct
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_fileBuffer.TryGetValue(fileName, out var result))
            {
                file = result.TryCast<T>();
                return;
            }

            if (!_dataBuffer.TryGetValue(fileName, out var data) || !data.IsValid)
            {
                file = new T();
                StoreData(fileName, file, options);
                return;
            }

            file = data.Read<T>();
            _fileBuffer.Update(fileName, data.Read<T>());
            _dataBuffer.Remove(fileName);
        }

        public void ResolveAsset<T>(string fileName, T asset, StoreOptions options = default)
            where T : ScriptableObject
        {
            if (TryGetAsset(fileName, asset))
            {
                return;
            }

            StoreAsset(fileName, asset, options);
        }

        #endregion


        #region Resolve

        public T Get<T>(string fileName)
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_fileBuffer.TryGetValue(fileName, out var result))
            {
                return (T) result;
            }

            if (!_dataBuffer.TryGetValue(fileName, out var data) || !data.IsValid)
            {
                return default(T);
            }

            var file = data.Read<T>();
            _fileBuffer.Update(fileName, data.Read<T>());
            _dataBuffer.Remove(fileName);
            SetDirty(fileName);
            return file;
        }

        public T GetFile<T>(string fileName) where T : class
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_fileBuffer.TryGetValue(fileName, out var result))
            {
                return (T) result;
            }

            if (!_dataBuffer.TryGetValue(fileName, out var data) || !data.IsValid)
            {
                return default(T);
            }

            var file = data.Read<T>();
            _fileBuffer.Update(fileName, data.Read<T>());
            _dataBuffer.Remove(fileName);
            SetDirty(fileName);
            return file;
        }

        public T GetData<T>(string fileName) where T : struct
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_fileBuffer.TryGetValue(fileName, out var result))
            {
                return (T) result;
            }

            if (!_dataBuffer.TryGetValue(fileName, out var data) || !data.IsValid)
            {
                return default(T);
            }

            var file = data.Read<T>();
            _fileBuffer.Update(fileName, data.Read<T>());
            _dataBuffer.Remove(fileName);
            SetDirty(fileName);
            return file;
        }

        public void GetAsset<T>(string fileName, T asset) where T : ScriptableObject
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_dataBuffer.TryGetValue(fileName, out var fileData) && fileData.IsValid)
            {
                JsonUtility.FromJsonOverwrite(fileData.RawData, asset);
#if UNITY_EDITOR
                FileSystem.Validator.ValidateScriptableObjectData(fileData.RawData, asset);
#endif
            }
            SetDirty(fileName);
        }

        #endregion


        #region Delete

        public void DeleteEntry(string fileName)
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            headerFiles.TryRemove(fileName, out var header);
            _dirtyFiles.Remove(header);
            _fileBuffer.Remove(fileName);
            _dataBuffer.Remove(fileName);

            var filePath = Path.Combine(folderName, fileName);

            FileSystem.Storage.Delete(filePath);
            FileSystem.Storage.Save(HeaderFilePath, this);
        }

        #endregion


        #region Reset Profile

        internal void ResetProfile()
        {
            headerFiles.Clear();
            _dirtyFiles.Clear();
            _dataBuffer.Clear();
            _fileBuffer.Clear();
        }

        #endregion


        #region Try Resolve

        public bool HasFile(string fileName)
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            return _fileBuffer.ContainsKey(fileName) || _dataBuffer.ContainsKey(fileName);
        }

        public bool TryGetFile<T>(string fileName, out T file) where T : class
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_fileBuffer.TryGetValue(fileName, out var result))
            {
                file = (T) result;
                return true;
            }

            if (!_dataBuffer.TryGetValue(fileName, out var data) || !data.IsValid)
            {
                file = default(T);
                return false;
            }

            file = data.Read<T>();
            _fileBuffer.Update(fileName, data.Read<T>());
            _dataBuffer.Remove(fileName);
            return true;
        }

        public bool TryGetData<T>(string fileName, out T file) where T : struct
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_fileBuffer.TryGetValue(fileName, out var result))
            {
                file = (T) result;
                return true;
            }

            if (!_dataBuffer.TryGetValue(fileName, out var data) || !data.IsValid)
            {
                file = default(T);
                return false;
            }

            file = data.Read<T>();
            _fileBuffer.Update(fileName, data.Read<T>());
            _dataBuffer.Remove(fileName);
            return true;
        }

        public bool TryGetAsset<T>(string fileName, T asset) where T : ScriptableObject
        {
            Assert.IsTrue(IsLoaded);
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_dataBuffer.TryGetValue(fileName, out var fileData) && fileData.IsValid)
            {
                JsonUtility.FromJsonOverwrite(fileData.RawData, asset);
                return true;
            }
            return false;
        }

        #endregion


        #region Saving

        public bool SetDirty(string fileName)
        {
            FileSystem.Validator.ValidateFileName(ref fileName);
            if (headerFiles.TryGetValue(fileName, out var headerFile))
            {
                headerFile.Update();
                headerFile.FileName = fileName;
                _dirtyFiles.Add(headerFile);
            }
            return false;
        }

        /// <summary>
        ///     Save every file in this slot.
        /// </summary>
        public void Save()
        {
            SaveFilesInternal(headerFiles.Values);
        }

        /// <summary>
        ///     Save every file marked as dirty in this slot.
        /// </summary>
        public void SaveDirty()
        {
            SaveFilesInternal(_dirtyFiles);
            _dirtyFiles.Clear();
        }

        public void SaveFile(string fileName)
        {
            FileSystem.Validator.ValidateFileName(ref fileName);

            version = FileSystem.Version;
            lastSaveTimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture);

            var filePath = Path.Combine(folderName, fileName);
            var header = headerFiles[fileName];
            header.Update();
            header.FileName = fileName;
            _dirtyFiles.Remove(header);

            if (_fileBuffer.TryGetValue(fileName, out var file))
            {
                FileSystem.Storage.Save(filePath, file);
                FileSystem.Storage.Save(HeaderFilePath, this);
                return;
            }

            if (_dataBuffer.TryGetValue(fileName, out var data) && data.IsValid)
            {
                FileSystem.Storage.Save(filePath, data.RawData);
                FileSystem.Storage.Save(HeaderFilePath, this);
            }
        }

        private void SaveFilesInternal(IEnumerable<FileHeader> files)
        {
            lastSaveTimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            version = FileSystem.Version;
            foreach (var header in files)
            {
                var fileName = header.FileName;
                var filePath = Path.Combine(folderName, fileName);

                header.Update();

                if (_fileBuffer.TryGetValue(fileName, out var file))
                {
                    FileSystem.Storage.Save(filePath, file);
                    continue;
                }

                if (_dataBuffer.TryGetValue(fileName, out var data) && data.IsValid)
                {
                    FileSystem.Storage.Save(filePath, data.RawData);
                }
            }
            FileSystem.Storage.Save(HeaderFilePath, this);
        }

        #endregion


        #region Loading & Unloading

        /// <summary>
        ///     Unload every file in this slot.
        /// </summary>
        internal void Unload()
        {
            _fileBuffer.Clear();
            _dataBuffer.Clear();
            _dirtyFiles.Clear();
            IsLoaded = false;
        }

        /// <summary>
        ///     Load every file in this slot.
        /// </summary>
        internal async UniTask LoadAsync()
        {
            _fileBuffer.Clear();
            _dataBuffer.Clear();

            var fileBuffer = ListPool<FileHeader>.Get();
            fileBuffer.AddRange(headerFiles.Values);

            for (var index = fileBuffer.Count - 1; index >= 0; index--)
            {
                var header = fileBuffer[index];
                await LoadFileAsync(header);
            }

            ListPool<FileHeader>.Release(fileBuffer);
            IsLoaded = true;
            FileSystem.Storage.Save(HeaderFilePath, this);

            async UniTask LoadFileAsync(FileHeader header)
            {
                switch (header.FileGroup)
                {
                    case FileGroup.Serializable:
                    {
                        await LoadSerializableFileAsync(header);
                        break;
                    }
                    case FileGroup.ScriptableObject:
                    {
                        await LoadAssetFileAsync(header);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            async UniTask LoadSerializableFileAsync(FileHeader header)
            {
                var filePath = Path.Combine(folderName, header.FileName);
                var fileType = Type.GetType(header.FileType);
                if (fileType != null)
                {
                    var fileData = await FileSystem.Storage.LoadAsync(filePath, fileType);
                    if (!fileData.IsValid)
                    {
                        Debug.LogWarning("File System",
                            $"Could not load file: {filePath}!\n Did you manually delete or move file system relevant files?");
                        headerFiles.Remove(header.FileName);
                        return;
                    }
                    _fileBuffer.AddUnique(header.FileName, fileData.Read());
                }
                else
                {
                    var fileData = await FileSystem.Storage.LoadAsync(filePath);
                    if (!fileData.IsValid)
                    {
                        Debug.LogWarning("File System",
                            $"Could not load file: {filePath}!\n Did you manually delete or move file system relevant files?");
                        headerFiles.Remove(header.FileName);
                        return;
                    }
                    _dataBuffer.AddUnique(header.FileName, fileData);
                }
            }

            async UniTask LoadAssetFileAsync(FileHeader header)
            {
                var filePath = Path.Combine(folderName, header.FileName);
                var assetData = await FileSystem.Storage.LoadAsync(filePath);
                if (!assetData.IsValid)
                {
                    Debug.LogWarning("File System",
                        $"Could not load file: {filePath}!\n Did you manually delete or move file system relevant files?");
                    headerFiles.Remove(header.FileName);
                    return;
                }
                _dataBuffer.AddUnique(header.FileName, assetData);
            }
        }

        /// <summary>
        ///     Load every file in this slot.
        /// </summary>
        internal void Load()
        {
            _fileBuffer.Clear();
            _dataBuffer.Clear();

            var fileBuffer = ListPool<FileHeader>.Get();
            fileBuffer.AddRange(headerFiles.Values);

            for (var index = fileBuffer.Count - 1; index >= 0; index--)
            {
                var header = fileBuffer[index];
                LoadFile(header);
            }

            ListPool<FileHeader>.Release(fileBuffer);
            IsLoaded = true;
            FileSystem.Storage.Save(HeaderFilePath, this);

            void LoadFile(FileHeader header)
            {
                switch (header.FileGroup)
                {
                    case FileGroup.Serializable:
                    {
                        LoadSerializableFile(header);
                        break;
                    }
                    case FileGroup.ScriptableObject:
                    {
                        LoadAssetFile(header);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            void LoadSerializableFile(FileHeader header)
            {
                var filePath = Path.Combine(folderName, header.FileName);
                var fileType = Type.GetType(header.FileType);
                if (fileType != null)
                {
                    var fileData = FileSystem.Storage.Load(filePath, fileType);
                    if (!fileData.IsValid)
                    {
                        Debug.LogWarning("File System",
                            $"Could not load file: {filePath}!\n Did you manually delete or move file system relevant files?");
                        headerFiles.Remove(header.FileName);
                        return;
                    }
                    _fileBuffer.AddUnique(header.FileName, fileData.Read());
                }
                else
                {
                    var fileData = FileSystem.Storage.Load(filePath);
                    if (!fileData.IsValid)
                    {
                        Debug.LogWarning("File System",
                            $"Could not load file: {filePath}!\n Did you manually delete or move file system relevant files?");
                        headerFiles.Remove(header.FileName);
                        return;
                    }
                    _dataBuffer.AddUnique(header.FileName, fileData);
                }
            }

            void LoadAssetFile(FileHeader header)
            {
                var filePath = Path.Combine(folderName, header.FileName);
                var assetData = FileSystem.Storage.Load(filePath);
                if (!assetData.IsValid)
                {
                    Debug.LogWarning("File System",
                        $"Could not load file: {filePath}!\n Did you manually delete or move file system relevant files?");
                    headerFiles.Remove(header.FileName);
                    return;
                }
                _dataBuffer.AddUnique(header.FileName, assetData);
            }
        }

        #endregion


        #region Ctor

        public Profile(string displayName, string folderName, string headerName) : this()
        {
            this.displayName = displayName;
            this.folderName = folderName;
            this.headerName = headerName;
            createdTimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            version = FileSystem.Version;

            headerFiles = new Map<string, FileHeader>();
        }

        private Profile()
        {
            _fileBuffer = new Dictionary<string, object>();
            _dataBuffer = new Dictionary<string, FileData>();
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public Profile Clone()
        {
            return (Profile) MemberwiseClone();
        }

        #endregion
    }
}