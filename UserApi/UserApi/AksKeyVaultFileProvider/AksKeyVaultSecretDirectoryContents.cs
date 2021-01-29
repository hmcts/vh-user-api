using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;

namespace UserApi.AksKeyVaultFileProvider
{
    public class AksKeyVaultSecretDirectoryContents : IDirectoryContents
    {
        private IEnumerable<IFileInfo> _entries;

        private readonly string _directory;

        public AksKeyVaultSecretDirectoryContents(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public bool Exists => Directory.Exists(_directory);

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            EnsureInitialized();
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            EnsureInitialized();
            return _entries.GetEnumerator();
        }

        private void EnsureInitialized()
        {
            try
            {
                _entries = new DirectoryInfo(_directory)
                    .EnumerateFileSystemInfos()
                    .Select<FileSystemInfo, IFileInfo>(info =>
                    {
                        if (info is FileInfo file)
                        {
                            return new AksKeyVaultSecretFileInfo(file);
                        }
                        else if (info is DirectoryInfo dir)
                        {
                            return new AksKeyVaultSecretDirectoryInfo(dir);
                        }

                        throw new InvalidOperationException("Unexpected type of FileSystemInfo");
                    });
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is IOException)
            {
                _entries = Enumerable.Empty<IFileInfo>();
            }
        }
    }
}