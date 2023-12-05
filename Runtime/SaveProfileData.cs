using System;
using System.Collections.Generic;

namespace MobX.Serialization
{
    public readonly ref struct SaveProfileData
    {
        public readonly string ProfileDataPath;
        public readonly string DisplayName;
        public readonly string FolderName;
        public readonly DateTime CreatedTimeStamp;
        internal readonly IReadOnlyList<Header> FileHeaders;

        internal SaveProfileData(string displayName, string folderName, DateTime createdTimeStamp,
            string profileDataPath, IReadOnlyList<Header> headers)
        {
            DisplayName = displayName;
            FolderName = folderName;
            CreatedTimeStamp = createdTimeStamp;
            ProfileDataPath = profileDataPath;
            FileHeaders = headers;
        }
    }
}