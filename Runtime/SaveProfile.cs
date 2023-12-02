﻿using Cysharp.Threading.Tasks;
using MobX.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace MobX.Serialization
{
    [Serializable]
    internal class SaveProfile : ISaveProfile
    {
        #region Fields

        public string DisplayName => profileDisplayName;
        public string FolderName => profileFolderName;
        public bool IsLoaded { get; private set; }
        public string ProfileFilePath => Path.Combine(profileFolderName, profileFileName);

        [SerializeField] private string profileDisplayName;
        [SerializeField] private string profileFolderName;
        [SerializeField] private string profileFileName;
        [SerializeField] private string createdTimeStamp;

        [SerializeField] private List<Header> files;

        private Dictionary<string, SaveData> _loadedSaveDataCache = new();
        private Dictionary<string, FileData> _loadedFileDataCache = new();
        private HashSet<string> _dirtySaveDataKeys = new();

        private bool _isDirty;

        #endregion


        #region API

        public void SaveFile<T>(string fileName, T value, StoreOptions options = default)
        {
            FileSystem.Validator.ValidateFileName(ref fileName);

            SaveData<T> saveData;

            if (_loadedSaveDataCache.TryGetValue(fileName, out var save))
            {
                saveData = save as SaveData<T>;
                if (saveData is not null)
                {
                    saveData.data = value;
                    saveData.lastSaveTimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    Debug.LogWarning("Save Profile", $"{fileName} was previously saved with a different type!");
                }
            }
            else
            {
                saveData = new SaveData<T>
                {
                    data = value,
                    fileName = fileName,
                    createdTimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    lastSaveTimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    qualifiedType = typeof(SaveData<T>).AssemblyQualifiedName,
                    fileSystemVersion = FileSystem.Version,
                    applicationVersion = Application.version,
                    tags = options.Tags
                };
                _loadedSaveDataCache.Add(fileName, saveData);
            }

            var filePath = Path.Combine(profileFolderName, fileName);
            FileSystem.Storage.Save(filePath, saveData);
            var header = new Header
            {
                fileName = fileName,
                qualifiedTypeName = typeof(SaveData<T>).AssemblyQualifiedName
            };
            if (files.AddUnique(header))
            {
                Debug.Log("Added unique header");
                var profileFilePath = ProfileFilePath;
                FileSystem.Validator.ValidateFileName(ref profileFilePath);
                FileSystem.Storage.Save(profileFilePath, this);
            }
        }

        public void StoreFile<T>(string fileName, T value, StoreOptions options = default)
        {
            FileSystem.Validator.ValidateFileName(ref fileName);

            SaveData<T> saveData;

            if (_loadedSaveDataCache.TryGetValue(fileName, out var save))
            {
                saveData = save as SaveData<T>;
                if (saveData is not null)
                {
                    saveData.data = value;
                    saveData.lastSaveTimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    Debug.LogWarning("Save Profile", $"{fileName} was previously saved with a different type!");
                }
            }
            else
            {
                saveData = new SaveData<T>
                {
                    data = value,
                    fileName = fileName,
                    lastSaveTimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    createdTimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    qualifiedType = typeof(SaveData<T>).AssemblyQualifiedName,
                    fileSystemVersion = FileSystem.Version,
                    applicationVersion = Application.version,
                    tags = options.Tags
                };
                _loadedSaveDataCache.Add(fileName, saveData);
            }

            _dirtySaveDataKeys.Add(fileName);
            _loadedFileDataCache.Remove(fileName);
            var header = new Header
            {
                fileName = fileName,
                qualifiedTypeName = typeof(SaveData<T>).AssemblyQualifiedName
            };
            if (files.AddUnique(header))
            {
                Debug.Log("Save Profile", $"Added unique header for [{fileName}]");
                _isDirty = true;
            }
        }

        public T LoadFile<T>(string fileName, StoreOptions options = default)
        {
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_loadedFileDataCache.TryGetValue(fileName, out var data))
            {
                var saveData = data.Read<SaveData<T>>();
                _loadedSaveDataCache.Add(fileName, saveData);
                _loadedFileDataCache.Remove(fileName);
                var value = saveData.data;
                return value;
            }

            if (_loadedSaveDataCache.TryGetValue(fileName, out var file))
            {
                var value = file is SaveData<T> save ? save.data : default(T);
                return value;
            }

            return default(T);
        }

        public void ResolveFile<T>(string fileName, ref T value, StoreOptions options = default)
        {
            FileSystem.Validator.ValidateFileName(ref fileName);

            if (_loadedFileDataCache.TryGetValue(fileName, out var data))
            {
                var saveData = data.Read<SaveData<T>>();
                _loadedSaveDataCache.Add(fileName, saveData);
                _loadedFileDataCache.Remove(fileName);
                value = saveData.data;
                return;
            }

            if (_loadedSaveDataCache.TryGetValue(fileName, out var file))
            {
                value = file is SaveData<T> save ? save.data : default(T);
                return;
            }

            value = Activator.CreateInstance<T>();
            SaveFile(fileName, value);
        }

        public void DeleteFile<T>(string fileName)
        {
            FileSystem.Validator.ValidateFileName(ref fileName);
        }

        public void Save()
        {
            foreach (var key in _dirtySaveDataKeys)
            {
                var saveData = _loadedSaveDataCache[key];
                var fileName = saveData.fileName;
                var filePath = Path.Combine(profileFolderName, fileName);
                FileSystem.Storage.Save(filePath, saveData);
            }
            if (_isDirty)
            {
                var profileFilePath = ProfileFilePath;
                FileSystem.Validator.ValidateFileName(ref profileFilePath);
                FileSystem.Storage.Save(profileFilePath, this);
            }
        }

        public async UniTask LoadAsync()
        {
            if (IsLoaded)
            {
                return;
            }
            foreach (var header in files)
            {
                var filePath = Path.Combine(profileFolderName, header.fileName);
                var type = Type.GetType(header.qualifiedTypeName);
                if (type != null && type.GetGenericTypeDefinition() == typeof(SaveData<>))
                {
                    var typedFileData = await FileSystem.Storage.LoadAsync(filePath, type);
                    _loadedSaveDataCache.Add(header.fileName, (SaveData) typedFileData.Read());
                }
                else
                {
                    var fileData = await FileSystem.Storage.LoadAsync(filePath);
                    _loadedFileDataCache.Add(header.fileName, fileData);
                }
            }
            IsLoaded = true;
        }

        public void Unload()
        {
            _loadedSaveDataCache.Clear();
            _loadedFileDataCache.Clear();
            _dirtySaveDataKeys.Clear();
        }

        #endregion


        #region Constructor

        public SaveProfile(string displayName, string folderName, string fileName)
        {
            profileDisplayName = displayName;
            profileFolderName = folderName;
            profileFileName = fileName;
            createdTimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}