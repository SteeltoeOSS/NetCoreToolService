// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Steeltoe.NetCoreToolService.Services;

namespace Steeltoe.NetCoreToolService.Archivers
{
    /// <summary>
    /// An <see cref="IArchiver"/> implementation using the ZIP archive file format.
    /// </summary>
    public class ZipArchiver : IArchiver
    {
        /* ----------------------------------------------------------------- *
         * properties                                                        *
         * ----------------------------------------------------------------- */

        public string Name => "zip";

        public string FileExtension => ".zip";

        public string MimeType => "application/zip";

        /* ----------------------------------------------------------------- *
         * Fix UNIX permissions in Zip archive extraction                    *
         *                                             Owner                 *
         *                                                 Group             *
         *                                                     Other         *
         *                                             r w r   r             *
         * ----------------------------------------------------------------- */
        private const int UnixFilePermissions = 0b_0000_0001_1010_0100_0000_0000_0000_0000;
        private const int UnixDirectoryPermissions = 0b_0000_0001_1110_1101_0000_0000_0000_0000;

        /* ----------------------------------------------------------------- *
         * fields                                                             *
         * ----------------------------------------------------------------- */

        private readonly CompressionLevel _compression;

        /* ----------------------------------------------------------------- *
         * constructors                                                      *
         * ----------------------------------------------------------------- */

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipArchiver"/> class.
        /// </summary>
        /// <param name="compression">Compression level default <see cref="CompressionLevel.Fastest"/>.</param>
        public ZipArchiver(CompressionLevel compression = CompressionLevel.Fastest)
        {
            _compression = compression;
        }

        /* ----------------------------------------------------------------- *
         * methods                                                           *
         * ----------------------------------------------------------------- */

        /// <inheritdoc/>
        public byte[] ToBytes(string path)
        {
            using var buffer = new MemoryStream();
            var archive = new ZipArchive(buffer, ZipArchiveMode.Create, true);
            AddPathToArchive(archive, path);
            archive.Dispose();
            buffer.Seek(0, SeekOrigin.Begin);
            return buffer.ToArray();
        }

        private void AddPathToArchive(ZipArchive archive, string rootPath, string path = null)
        {
            path ??= rootPath;
            var directory = new DirectoryInfo(path);
            if (path != rootPath)
            {
                var entry = archive.CreateEntry($"{Path.GetRelativePath(rootPath, path)}/");
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    entry.ExternalAttributes = UnixDirectoryPermissions;
                }
            }

            foreach (var file in directory.GetFiles())
            {
                var entry = archive.CreateEntry(Path.GetRelativePath(rootPath, file.FullName), _compression);
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    entry.ExternalAttributes = UnixFilePermissions;
                }

                using var fileStream = File.Open(file.FullName, FileMode.Open);
                using var entryStream = entry.Open();
                fileStream.CopyTo(entryStream);
            }

            foreach (var subDirectory in directory.GetDirectories())
            {
                AddPathToArchive(archive, rootPath, subDirectory.FullName);
            }
        }
    }
}
