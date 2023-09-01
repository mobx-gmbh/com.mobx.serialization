using UnityEngine;

namespace MobX.Serialization
{
    public interface IProfile
    {
        #region Information

        bool IsLoaded { get; }

        string DisplayName { get; }

        string FolderName { get; }

        string Version { get; }

        #endregion


        #region Store

        void Store<T>(string fileName, T file, StoreOptions options = default);

        void StoreFile<T>(string fileName, T file, StoreOptions options = default) where T : class;

        void StoreData<T>(string fileName, T data, StoreOptions options = default) where T : struct;

        void StoreAsset<T>(string fileName, T asset, StoreOptions options = default) where T : ScriptableObject;

        #endregion


        #region Resolve

        void Resolve<T>(string fileName, out T file, StoreOptions options = default) where T : new();

        void ResolveFile<T>(string fileName, out T file, StoreOptions options = default) where T : class, new();

        void ResolveData<T>(string fileName, out T file, StoreOptions options = default) where T : struct;

        void ResolveAsset<T>(string fileName, T asset, StoreOptions options = default) where T : ScriptableObject;

        #endregion


        #region Get

        T Get<T>(string fileName);

        T GetFile<T>(string fileName) where T : class;

        T GetData<T>(string fileName) where T : struct;

        void GetAsset<T>(string fileName, T asset) where T : ScriptableObject;

        bool TryGetFile<T>(string fileName, out T file) where T : class;

        bool TryGetData<T>(string fileName, out T file) where T : struct;

        bool TryGetAsset<T>(string fileName, T asset) where T : ScriptableObject;

        #endregion


        #region Delete & Validation Methods

        void DeleteEntry(string fileName);

        bool HasFile(string fileName);

        public bool SetDirty(string fileName);

        #endregion


        #region Saving

        void Save();

        void SaveDirty();

        void SaveFile(string fileName);

        #endregion
    }
}
