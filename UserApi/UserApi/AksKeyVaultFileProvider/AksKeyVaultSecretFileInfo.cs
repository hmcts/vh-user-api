using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace UserApi.AksKeyVaultFileProvider
{
    public class AksKeyVaultSecretFileInfo : IFileInfo
    {
        private readonly FileInfo _info;

        public AksKeyVaultSecretFileInfo(FileInfo info)
        {
            _info = info;
        }

        public bool Exists => _info.Exists;

        public long Length => _info.Length;

        public string PhysicalPath => _info.FullName;

        public string Name => _info.Name.Replace("--", "__");

        public DateTimeOffset LastModified => _info.LastWriteTimeUtc;

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            var bufferSize = 1;
            return new FileStream(
                PhysicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
    }
}