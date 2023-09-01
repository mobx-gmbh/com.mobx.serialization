using System;

namespace MobX.Serialization
{
    public class FileSystemNotInitializedException : Exception
    {
        public FileSystemNotInitializedException(string access) : base(
            $"Access {access} before file system is initialized!")
        {
        }
    }
}