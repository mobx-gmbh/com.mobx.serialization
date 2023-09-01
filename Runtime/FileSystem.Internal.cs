using Cysharp.Threading.Tasks;
using MobX.Utilities;
using MobX.Utilities.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MobX.Serialization
{
    public static partial class FileSystem
    {
        #region Private Properties & Fields

        public static string RootFolder { get; private set; }

        internal static IFileStorage Storage { get; private set; }
        internal static readonly LogCategory Log = "File System";

        private static Profile activeProfile;
        private static Profile sharedProfile;

        private static FileValidator validator;

        private static ProfilePathData profilePathData;
        private static Dictionary<string, IProfile> profileCache;

        private const string DefaultEncryptionKey = "QplEVJveOQ";

        private const string FileSystemDataSave = "storage.sav";
        private const string ProfileHeader = "_slot.sav";

        private const string SharedProfileName = "Shared";
        private const string SharedProfileFolder = "_shared";
        private const string SharedProfileHeader = "_shared.sav";
        private const string ProfilePathsKey = "profiles.sav";

        private static string defaultProfileName;

        private const int MaxProfileNameLength = 64;
        private static uint profileLimit;

        private static string version = string.Empty;
        private static FileSystemData fileSystemData;

        #endregion


        #region Platform Storage Provider

        private static IFileStorage CreateFileStorage(in FileSystemArgs args)
        {
            Debug.Log("File System", "Creating File Storage");
            RootFolder = args.rootFolder.IsNotNullOrWhitespace() ? args.rootFolder : string.Empty;
            var encryptionProvider = args.EncryptionProvider ?? args.encryptionAsset.ValueOrDefault();
            var encryptionKey = args.encryptionKey.TryGetValue(out var key) && key.IsNotNullOrWhitespace()
                ? args.encryptionKey.Value
                : DefaultEncryptionKey;

            var provider = ArrayUtility.Cast<EncryptionProviderAsset, IEncryptionProvider>(args.encryptionProvider);

            var fileOperations = args.fileStorageProvider.ValueOrDefault() as IFileOperations ??
                new MonoFileOperations();

            var fileStorageArguments = new FileStorageArguments(
                RootFolder,
                encryptionKey,
                encryptionProvider,
                args.exceptions,
                provider,
                args.forceSynchronous,
                fileOperations
            );

            var storage = new FileStorage();
            storage.Initialize(fileStorageArguments);

            Debug.Log("File System", $"Created File Storage! Using {fileOperations} file operation interface!");
            return storage;
        }

        #endregion


        #region Initialization

        private static async UniTask InitializeAsyncInternal(FileSystemArgs args)
        {
            try
            {
                if (State != FileSystemState.Uninitialized)
                {
                    return;
                }
                if (args.forceSynchronous)
                {
                    InitializeInternal(args);
                    return;
                }

                Debug.Log(Log, "Initialization Started");
                State = FileSystemState.Initializing;

                try
                {
                    InitializationStarted?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                profileLimit = args.profileLimit.ValueOrDefault() > 0 ? args.profileLimit : uint.MaxValue;
                version = args.version ?? string.Empty;
                validator = new FileValidator(args);
                Storage = CreateFileStorage(args);
                defaultProfileName = args.defaultProfileName;

                var sharedProfilePath = Path.Combine(SharedProfileFolder, SharedProfileHeader);
                var sharedProfileData = await Storage.LoadAsync<Profile>(sharedProfilePath);
                sharedProfile = sharedProfileData.IsValid
                    ? sharedProfileData.Read()
                    : new Profile(SharedProfileName, SharedProfileFolder, SharedProfileHeader);

                await sharedProfile.LoadAsync();

                var activeProfilePath = sharedProfile.TryGetData(FileSystemDataSave, out fileSystemData)
                    ? fileSystemData.activeProfileFilePath
                    : string.Empty;

                sharedProfile.ResolveFile(ProfilePathsKey, out profilePathData);
                var profilePaths = profilePathData.Paths;

                var profileData = await Storage.LoadAsync<Profile>(activeProfilePath);
                var profile = profileData.IsValid ? profileData.Read() : CreateDefaultProfile(profilePaths);

                static Profile CreateDefaultProfile(ICollection paths)
                {
                    var folderName = $"{defaultProfileName}{paths.Count.ToString()}";
                    return new Profile(folderName, folderName, ProfileHeader);
                }

                profileCache = new Dictionary<string, IProfile>(profilePaths.Count);
                foreach (var profilePath in profilePaths)
                {
                    if (profileCache.ContainsKey(profilePath))
                    {
                        continue;
                    }
                    var fileData = await Storage.LoadAsync<Profile>(profilePath);
                    if (fileData.IsValid)
                    {
                        profileCache.AddUnique(profilePath, fileData.Read());
                    }
                }

                await UpdateActiveProfileAsyncInternal(profile);

                State = FileSystemState.Initialized;
                var converter = args.saveGameConverter.ValueOrDefault();
                if (converter != null)
                {
                    var topLevelStorageProvider = CreateFileStorage(converter.FileSystemArgs);
                    await converter.ConvertAsync(topLevelStorageProvider, profile, sharedProfile);
                    profile.Save();
                }

                sharedProfile.Save();

                Debug.Log(Log, "Initialization Completed");
            }
            catch (Exception exception)
            {
                Debug.LogException(Log, new Exception("Error during file system initialization. Shutdown", exception));
                await ShutdownAsync();
                return;
            }

            InitializationCompleted?.Invoke();
        }

        private static void InitializeInternal(in FileSystemArgs args = new())
        {
            try
            {
                if (State != FileSystemState.Uninitialized)
                {
                    return;
                }

                Debug.Log(Log, "Initialization Started");
                State = FileSystemState.Initializing;

                try
                {
                    InitializationStarted?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                profileLimit = args.profileLimit.ValueOrDefault() > 0 ? args.profileLimit : uint.MaxValue;
                version = args.version ?? string.Empty;
                validator = new FileValidator(args);
                Storage = CreateFileStorage(args);
                defaultProfileName = args.defaultProfileName;

                var sharedProfilePath = Path.Combine(SharedProfileFolder, SharedProfileHeader);
                var sharedProfileData = Storage.Load<Profile>(sharedProfilePath);
                sharedProfile = sharedProfileData.IsValid
                    ? sharedProfileData.Read()
                    : new Profile(SharedProfileName, SharedProfileFolder, SharedProfileHeader);

                sharedProfile.Load();

                var activeProfilePath = sharedProfile.TryGetData(FileSystemDataSave, out fileSystemData)
                    ? fileSystemData.activeProfileFilePath
                    : string.Empty;

                sharedProfile.ResolveFile(ProfilePathsKey, out profilePathData);
                var profilePaths = profilePathData.Paths;

                var profileData = Storage.Load<Profile>(activeProfilePath);
                var profile = profileData.IsValid ? profileData.Read() : CreateDefaultProfile(profilePaths);

                static Profile CreateDefaultProfile(ICollection paths)
                {
                    var folderName = $"{defaultProfileName}{paths.Count.ToString()}";
                    return new Profile(folderName, folderName, ProfileHeader);
                }

                profileCache = new Dictionary<string, IProfile>(profilePaths.Count);
                foreach (var profilePath in profilePaths)
                {
                    if (profileCache.ContainsKey(profilePath))
                    {
                        continue;
                    }
                    var fileData = Storage.Load<Profile>(profilePath);
                    if (fileData.IsValid)
                    {
                        profileCache.AddUnique(profilePath, fileData.Read());
                    }
                }

                UpdateActiveProfileInternal(profile);

                State = FileSystemState.Initialized;

                var converter = args.saveGameConverter.ValueOrDefault();
                if (converter != null)
                {
                    var converterArgs = converter.FileSystemArgs;
                    converterArgs.forceSynchronous = true;
                    var topLevelStorageProvider = CreateFileStorage(converterArgs);
                    converter.Convert(topLevelStorageProvider, profile, sharedProfile);
                    profile.Save();
                }

                sharedProfile.Save();

                Debug.Log(Log, "Initialization Completed");
            }
            catch (Exception exception)
            {
                Debug.LogException(Log, new Exception("Error during file system initialization. Shutdown", exception));
                Shutdown();
                return;
            }

            InitializationCompleted?.Invoke();
        }

        #endregion


        #region Shutdown

        private static async UniTask ShutdownAsyncInternal(FileSystemShutdownArgs args)
        {
            if (State != FileSystemState.Initialized)
            {
                return;
            }

            State = FileSystemState.Shutdown;
            Debug.Log(Log, "Shutdown Started");
            try
            {
                ShutdownStarted?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            await Storage.ShutdownAsync(args);
            activeProfile.Unload();
            sharedProfile.Unload();
            Storage = null;
            RootFolder = null;
            profileCache = null;
            profilePathData = null;
            validator = null;
            defaultProfileName = null;
            sharedProfile = null;
            activeProfile = null;
            version = string.Empty;
            fileSystemData = default(FileSystemData);

            State = FileSystemState.Uninitialized;
            Debug.Log(Log, "Shutdown Completed");
            ShutdownCompleted?.Invoke();
        }

        private static void ShutdownInternal(in FileSystemShutdownArgs args)
        {
            if (State != FileSystemState.Initialized)
            {
                return;
            }

            State = FileSystemState.Shutdown;
            Debug.Log(Log, "Shutdown Started");
            try
            {
                ShutdownStarted?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            Storage.Shutdown(args);
            activeProfile.Unload();
            sharedProfile.Unload();
            Storage = null;
            RootFolder = null;
            profilePathData = null;
            profileCache = null;
            validator = null;
            defaultProfileName = null;
            sharedProfile = null;
            activeProfile = null;
            version = string.Empty;
            fileSystemData = default(FileSystemData);
            State = FileSystemState.Uninitialized;
            Debug.Log(Log, "Shutdown Completed");
            ShutdownCompleted?.Invoke();
        }

        #endregion


        #region Profile Creation

        private static async UniTask<ProfileCreationResult> CreateProfileAsyncInternal(ProfileCreationArgs args)
        {
            if (State != FileSystemState.Initialized)
            {
                throw new FileSystemNotInitializedException(nameof(CreateProfileAsync));
            }

            if (profilePathData.Paths.Count >= profileLimit)
            {
                return ProfileCreationResult.ProfileLimitReached;
            }

            if (Regex.IsMatch(args.name ?? string.Empty, $"{defaultProfileName}\\d*$"))
            {
                return ProfileCreationResult.SystemReservedName;
            }

            var profileName = args.name.IsNotNullOrWhitespace()
                ? args.name!
                : CreateFallbackName();

            string CreateFallbackName()
            {
                var result = defaultProfileName + ++fileSystemData.nextProfileIndex;
                sharedProfile.StoreData(FileSystemDataSave, fileSystemData);
                return result;
            }

            if (profileName.Length > MaxProfileNameLength)
            {
                return ProfileCreationResult.NameToLong;
            }
            if (!Validator.IsValidProfileName(profileName))
            {
                return ProfileCreationResult.NameInvalid;
            }

            var fileSystemName = profileName.Replace(' ', '_');
            var profilePathCandidate = Path.Combine(fileSystemName, ProfileHeader);
            if (profilePathData.Paths.Contains(profilePathCandidate))
            {
                return ProfileCreationResult.NameNotAvailable;
            }

            var profile = new Profile(profileName, fileSystemName, ProfileHeader);

            profilePathData.Paths.AddUnique(profile.HeaderFilePath);

            if (args.activate)
            {
                await UpdateActiveProfileAsyncInternal(profile);
            }

            profile.Save();
            sharedProfile.Save();
            ProfileCreated?.Invoke(profile, args);
            return new ProfileCreationResult(profile, ProfileCreationStatus.Success);
        }

        private static ProfileCreationResult CreateProfileInternal(ProfileCreationArgs args)
        {
            if (State != FileSystemState.Initialized)
            {
                throw new FileSystemNotInitializedException(nameof(CreateProfileAsync));
            }

            if (profilePathData.Paths.Count >= profileLimit)
            {
                return ProfileCreationResult.ProfileLimitReached;
            }

            if (Regex.IsMatch(args.name ?? string.Empty, $"{defaultProfileName}\\d*$"))
            {
                return ProfileCreationResult.SystemReservedName;
            }

            var profileName = args.name.IsNotNullOrWhitespace()
                ? args.name!
                : CreateFallbackName();

            string CreateFallbackName()
            {
                var result = defaultProfileName + ++fileSystemData.nextProfileIndex;
                sharedProfile.StoreData(FileSystemDataSave, fileSystemData);
                sharedProfile.Save();
                return result;
            }

            if (profileName.Length > MaxProfileNameLength)
            {
                return ProfileCreationResult.NameToLong;
            }
            if (!Validator.IsValidProfileName(profileName))
            {
                return ProfileCreationResult.NameInvalid;
            }

            var fileSystemName = profileName.Replace(' ', '_');
            var profilePathCandidate = Path.Combine(fileSystemName, ProfileHeader);
            if (profilePathData.Paths.Contains(profilePathCandidate))
            {
                return ProfileCreationResult.NameNotAvailable;
            }

            var profile = new Profile(profileName, fileSystemName, ProfileHeader);

            profilePathData.Paths.AddUnique(profile.HeaderFilePath);

            if (args.activate)
            {
                UpdateActiveProfileInternal(profile);
            }

            profile.Save();
            ProfileCreated?.Invoke(profile, args);
            return new ProfileCreationResult(profile, ProfileCreationStatus.Success);
        }

        #endregion


        #region Profile Switching

        private static async UniTask<bool> UpdateActiveProfileAsyncInternal(Profile profile)
        {
            if (profile == null)
            {
                return false;
            }
            if (profile == activeProfile)
            {
                return false;
            }
            if (activeProfile is { IsLoaded: true })
            {
                activeProfile.Unload();
            }
            Debug.Log(Log, $"Switch active profile from: {activeProfile.ToNullString()} to {profile}");
            activeProfile = profile;
            await activeProfile.LoadAsync();
            profileCache.Update(activeProfile.HeaderFilePath, activeProfile);
            fileSystemData.activeProfileFilePath = profile.HeaderFilePath;
            sharedProfile.StoreData(FileSystemDataSave, fileSystemData);
            if (IsInitialized)
            {
                ProfileChanged?.Invoke(activeProfile);
            }
            activeProfile.Save();
            sharedProfile.Save();
            return true;
        }

        private static bool UpdateActiveProfileInternal(Profile profile)
        {
            if (profile == null)
            {
                return false;
            }
            if (profile == activeProfile)
            {
                return false;
            }
            if (activeProfile is { IsLoaded: true })
            {
                activeProfile.Unload();
            }
            Debug.Log(Log, $"Switch active profile from: {activeProfile.ToNullString()} to {profile}");
            activeProfile = profile;
            activeProfile.Load();
            profileCache.Update(activeProfile.HeaderFilePath, activeProfile);
            fileSystemData.activeProfileFilePath = profile.HeaderFilePath;
            sharedProfile.StoreData(FileSystemDataSave, fileSystemData);
            if (IsInitialized)
            {
                ProfileChanged?.Invoke(activeProfile);
            }
            activeProfile.Save();
            sharedProfile.Save();
            return true;
        }

        #endregion


        #region Profile Deletion (Profile)

        private static async UniTask DeleteProfileAsyncInternal(Profile profile)
        {
            if (profile == null)
            {
                return;
            }
            if (profile == activeProfile)
            {
                return;
            }

            ProfileDeleted?.Invoke(profile);

            profileCache.Remove(profile.HeaderFilePath);
            profilePathData.Paths.Remove(profile.HeaderFilePath);

            if (profile.IsLoaded)
            {
                profile.Unload();
            }

            foreach (var header in profile.FileHeaders)
            {
                var filePath = Path.Combine(profile.FolderName, header.FileName);
                await Storage.DeleteAsync(filePath);
            }

            await Storage.DeleteAsync(profile.HeaderFilePath);
            await Storage.DeleteFolderAsync(profile.FolderName);
        }

        private static async UniTask DeleteProfileAsyncInternal(string profileName)
        {
            var profile = GetProfileByName(profileName);
            await DeleteProfileAsyncInternal(profile as Profile);
        }

        private static void DeleteProfileInternal(Profile profile)
        {
            if (profile == null)
            {
                return;
            }
            if (profile == activeProfile)
            {
                return;
            }

            ProfileDeleted?.Invoke(profile);

            profileCache.Remove(profile.HeaderFilePath);
            profilePathData.Paths.Remove(profile.HeaderFilePath);

            if (profile.IsLoaded)
            {
                profile.Unload();
            }

            foreach (var header in profile.FileHeaders)
            {
                var filePath = Path.Combine(profile.FolderName, header.FileName);
                Storage.Delete(filePath);
            }

            Storage.Delete(profile.HeaderFilePath);
            Storage.DeleteFolder(profile.FolderName);
        }

        private static void DeleteProfileInternal(string profileName)
        {
            var profile = GetProfileByName(profileName);
            DeleteProfileInternal(profile as Profile);
        }

        private static IProfile GetProfileByName(string profileName)
        {
            foreach (var profile in Profiles)
            {
                if (profile.DisplayName != profileName)
                {
                    continue;
                }
                return profile;
            }
            return null;
        }

        #endregion


        #region Reset Profile

        private static async UniTask ResetProfileAsyncInternal(Profile profile)
        {
            if (profile == null)
            {
                return;
            }

            if (profile.IsLoaded)
            {
                profile.Unload();
            }

            foreach (var header in profile.FileHeaders)
            {
                var filePath = Path.Combine(profile.FolderName, header.FileName);
                await Storage.DeleteAsync(filePath);
            }

            profile.ResetProfile();
            profile.Save();

            ProfileReset?.Invoke(profile);
        }

        private static async UniTask ResetProfileAsyncInternal(string profileName)
        {
            var profile = GetProfileByName(profileName);
            await ResetProfileAsyncInternal(profile as Profile);
        }

        private static void ResetProfileInternal(Profile profile)
        {
            if (profile == null)
            {
                return;
            }

            if (profile.IsLoaded)
            {
                profile.Unload();
            }

            foreach (var header in profile.FileHeaders)
            {
                var filePath = Path.Combine(profile.FolderName, header.FileName);
                Storage.Delete(filePath);
            }

            profile.ResetProfile();
            profile.Save();

            ProfileReset?.Invoke(profile);
        }

        private static void ResetProfileInternal(string profileName)
        {
            var profile = GetProfileByName(profileName);
            ResetProfileInternal(profile as Profile);
        }

        #endregion


        #region Profile Backup

#pragma warning disable CS1998
        private static async UniTask<ProfileBackup> BackupProfileInternalAsync(IProfile profile)
#pragma warning restore CS1998
        {
            if (profile is not Profile original)
            {
                return default(ProfileBackup);
            }

            var copy = original.Clone();
            copy.Save();
            return default(ProfileBackup);
        }

        #endregion
    }
}
