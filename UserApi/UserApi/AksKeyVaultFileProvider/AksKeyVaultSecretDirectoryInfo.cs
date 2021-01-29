using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace UserApi.AksKeyVaultFileProvider
{
    public class AksKeyVaultSecretDirectoryInfo : IFileInfo
    {
        private readonly DirectoryInfo _info;

        public AksKeyVaultSecretDirectoryInfo(DirectoryInfo info)
        {
            _info = info;
        }

        public bool Exists => _info.Exists;

        public long Length => -1;

        public string PhysicalPath => _info.FullName;

        public string Name => _info.Name;

        public DateTimeOffset LastModified => _info.LastWriteTimeUtc;

        public bool IsDirectory => true;

        public Stream CreateReadStream()
        {
            throw new InvalidOperationException("Cannot create a stream for a directory.");
        }
    }
}