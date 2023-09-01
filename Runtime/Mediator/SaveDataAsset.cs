using MobX.Mediator.Events;
using MobX.Mediator.Values;
using MobX.Utilities.Inspector;
using MobX.Utilities.Types;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MobX.Serialization.Mediator
{
    /// <summary>
    ///     Asset represents a value that can be saved and loaded to the persistent data storage.
    /// </summary>
    /// <typeparam name="T">The type of the saved value. Must be serializable!</typeparam>
    public abstract class SaveDataAsset<T> : ValueAsset<T>
    {
        #region Inspector & Data

        [NonSerialized] private Storage<T> _storage;
        [SerializeField] private RuntimeGUID guid;
        [SerializeField] private T defaultValue;
        [Tooltip("Automatically saves the data every time it is updated")]
        [SerializeField] private bool autoSave;
        [Tooltip("The level to store the data on. Either profile specific or shared between profiles")]
        [SerializeField] private StorageLevel storageLevel = StorageLevel.Profile;
        [Tooltip("The key used to save the value. Only use asset name for debugging!")]
        [SerializeField] private KeyType key;
        private readonly Broadcast<T> _changedEvent = new();

        /// <summary>
        ///     Event, called every time the value changed.
        /// </summary>
        public sealed override event Action<T> Changed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            add => _changedEvent.Add(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            remove => _changedEvent.Remove(value);
        }

        public RuntimeGUID GUID => guid;

        private string Key => key switch
        {
            KeyType.AssetGUID => guid.ToString(),
            KeyType.AssetName => name,
            var _ => throw new ArgumentOutOfRangeException()
        };

        private IProfile Profile => storageLevel switch
        {
            StorageLevel.Profile => FileSystem.Profile,
            StorageLevel.Shared => FileSystem.SharedProfile,
            _ => throw new ArgumentOutOfRangeException()
        };

        private enum KeyType
        {
            AssetGUID = 0,
            AssetName = 1
            // TODO: Add Custom GUID
        }

        #endregion


        #region Value

        [ShowInInspector]
        public override T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public override void SetValue(T newValue)
        {
            var isEqual = EqualityComparer<T>.Default.Equals(newValue, _storage.value);
            if (isEqual)
            {
                return;
            }
            _storage.value = newValue;
            _changedEvent.Raise(newValue);
            var profile = Profile;
            profile.Store(Key, _storage);
            if (autoSave)
            {
                profile.SaveFile(Key);
            }
        }

        public override T GetValue()
        {
            return _storage.value;
        }

        protected ref T GetValueRef()
        {
            return ref _storage.value;
        }

        #endregion


        #region Setup

        protected override void OnEnable()
        {
            base.OnEnable();
            FileSystem.InitializationCompleted += OnFileSystemInitialized;
            FileSystem.ProfileChanged += OnProfileChanged;

            if (FileSystem.IsInitialized)
            {
                Load();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            FileSystem.InitializationCompleted -= OnFileSystemInitialized;
            FileSystem.ProfileChanged -= OnProfileChanged;
        }

        private void OnFileSystemInitialized()
        {
            Load();
        }

        private void OnProfileChanged(IProfile profile)
        {
            Load();
        }

        #endregion


        #region IO Operations

        [Button]
        [DrawLine]
        public void Save()
        {
            var profile = Profile;

            profile.Store(Key, _storage);
            profile.SaveFile(Key);
        }

        [Button]
        public void Load()
        {
            var profile = Profile;

            if (profile.HasFile(Key))
            {
                profile.ResolveData(Key, out _storage);
                return;
            }

            _storage = new Storage<T>(defaultValue);
        }

        [Button("Reset")]
        public void ResetData()
        {
            var profile = Profile;
            profile.DeleteEntry(Key);
            _storage = new Storage<T>(defaultValue);
        }

        #endregion
    }
}