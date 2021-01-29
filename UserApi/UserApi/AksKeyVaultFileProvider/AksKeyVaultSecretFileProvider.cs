using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace UserApi.AksKeyVaultFileProvider
{
    public class AksKeyVaultSecretFileProvider : IFileProvider
    {
        public string Root { get; }

        private static readonly char[] _pathSeparators = new[]
            {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};

        public AksKeyVaultSecretFileProvider(string root)
        {
            if (!Path.IsPathRooted(root))
            {
                throw new ArgumentException("The path must be absolute.", nameof(root));
            }

            Root = Path.GetFullPath(root);
            if (!Directory.Exists(Root))
            {
                throw new DirectoryNotFoundException(Root);
            }
        }

        private string GetFullPath(string path)
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Combine(Root, path));
            }
            catch
            {
                return null;
            }

            if (!IsUnderneathRoot(fullPath))
            {
                return null;
            }

            return fullPath;
        }

        private bool IsUnderneathRoot(string fullPath)
        {
            return fullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (string.IsNullOrEmpty(subpath))
            {
                return new NotFoundFileInfo(subpath);
            }

            subpath = subpath.TrimStart(_pathSeparators);
            if (Path.IsPathRooted(subpath))
            {
                return new NotFoundFileInfo(subpath);
            }

            var fullPath = GetFullPath(subpath);
            if (fullPath == null)
            {
                return new NotFoundFileInfo(subpath);
            }

            var fileInfo = new FileInfo(fullPath);

            return new AksKeyVaultSecretFileInfo(fileInfo);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            try
            {
                if (subpath == null)
                {
                    return NotFoundDirectoryContents.Singleton;
                }

                subpath = subpath.TrimStart(_pathSeparators);
                if (Path.IsPathRooted(subpath))
                {
                    return NotFoundDirectoryContents.Singleton;
                }

                var fullPath = GetFullPath(subpath);
                if (fullPath == null || !Directory.Exists(fullPath))
                {
                    return NotFoundDirectoryContents.Singleton;
                }

                return new AksKeyVaultSecretDirectoryContents(fullPath);
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (IOException)
            {
            }
            return NotFoundDirectoryContents.Singleton;
        }

        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }
}