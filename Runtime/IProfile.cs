// using UnityEngine;
//
// namespace MobX.Serialization
// {
//     public interface IProfile
//     {
//         #region Information
//
//         bool IsLoaded { get; }
//
//         string DisplayName { get; }
//
//         string FolderName { get; }
//
//         string Version { get; }
//
//         #endregion
//
//
//         #region Store
//
//         void Store<T>(string fileName, T file, StoreOptions options = default);
//
//         void StoreFile<T>(string fileName, T file, StoreOptions options = default) where T : class;
//
//         void StoreData<T>(string fileName, T data, StoreOptions options = default) where T : struct;
//
//         void StoreAsset<T>(string fileName, T asset, StoreOptions options = default) where T : ScriptableObject;
//
//         void StoreData(string fileName, int data, StoreOptions options = default);
//
//         void StoreData(string fileName, long data, StoreOptions options = default);
//
//         void StoreData(string fileName, float data, StoreOptions options = default);
//
//         void StoreData(string fileName, double data, StoreOptions options = default);
//
//         void StoreData(string fileName, byte data, StoreOptions options = default);
//
//         void StoreData(string fileName, short data, StoreOptions options = default);
//
//         void StoreData(string fileName, bool data, StoreOptions options = default);
//
//         #endregion
//
//
//         #region Resolve
//
//         void Resolve<T>(string fileName, out T file, StoreOptions options = default) where T : new();
//
//         void ResolveFile<T>(string fileName, out T file, StoreOptions options = default) where T : class, new();
//
//         void ResolveData<T>(string fileName, out T file, StoreOptions options = default) where T : struct;
//
//         void ResolveAsset<T>(string fileName, T asset, StoreOptions options = default) where T : ScriptableObject;
//
//         void ResolveData(string fileName, out int data, StoreOptions options = default);
//
//         void ResolveData(string fileName, out long data, StoreOptions options = default);
//
//         void ResolveData(string fileName, out float data, StoreOptions options = default);
//
//         void ResolveData(string fileName, out double data, StoreOptions options = default);
//
//         void ResolveData(string fileName, out byte data, StoreOptions options = default);
//
//         void ResolveData(string fileName, out short data, StoreOptions options = default);
//
//         void ResolveData(string fileName, out bool data, StoreOptions options = default);
//
//         #endregion
//
//
//         #region Get
//
//         T Get<T>(string fileName);
//
//         T GetFile<T>(string fileName) where T : class;
//
//         T GetData<T>(string fileName) where T : struct;
//
//         int GetData(string fileName);
//
//         void GetAsset<T>(string fileName, T asset) where T : ScriptableObject;
//
//         bool TryGetFile<T>(string fileName, out T file) where T : class;
//
//         bool TryGetData<T>(string fileName, out T file) where T : struct;
//
//         bool TryGetAsset<T>(string fileName, T asset) where T : ScriptableObject;
//
//         bool TryGetData(string fileName, out int data);
//
//         #endregion
//
//
//         #region Delete & Validation Methods
//
//         void DeleteEntry(string fileName);
//
//         bool HasFile(string fileName);
//
//         public bool SetDirty(string fileName);
//
//         #endregion
//
//
//         #region Saving
//
//         void Save();
//
//         void SaveDirty();
//
//         void SaveFile(string fileName);
//
//         #endregion
//     }
// }

