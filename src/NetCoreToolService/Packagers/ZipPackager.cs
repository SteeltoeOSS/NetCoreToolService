// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Steeltoe.NetCoreToolService.Packagers;

/// <summary>
/// An <see cref="IPackager" /> implementation using the ZIP archive file format.
/// </summary>
public sealed class ZipPackager : IPackager
{
    /* ----------------------------------------------------------------- *
     * Fix UNIX permissions in Zip archive extraction                    *
     *                                             Owner                 *
     *                                                 Group             *
     *                                                     Other         *
     *                                             r w r   r             *
     * ----------------------------------------------------------------- */
    private const int UnixFilePermissions = 0b_0000_0001_1010_0100_0000_0000_0000_0000;
    private const int UnixDirectoryPermissions = 0b_0000_0001_1110_1101_0000_0000_0000_0000;

    private const CompressionLevel Level = CompressionLevel.SmallestSize;

    /// <summary>
    /// Gets the name of the ZipArchiver ("zip").
    /// </summary>
    public string Name => "zip";

    /// <summary>
    /// Gets the file extension for the ZipArchiver (".zip").
    /// </summary>
    public string FileExtension => ".zip";

    /// <summary>
    /// Gets the MIME type for the ZipArchiver ("application/zip").
    /// </summary>
    public string MimeType => "application/zip";

    /// <inheritdoc />
    public byte[] ToBytes(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            AddPathToArchive(archive, path, null);
        }

        buffer.Seek(0, SeekOrigin.Begin);
        return buffer.ToArray();
    }

    private static void AddPathToArchive(ZipArchive archive, string rootPath, string? path)
    {
        path ??= rootPath;
        var directory = new DirectoryInfo(path);

        if (path != rootPath)
        {
            ZipArchiveEntry entry = archive.CreateEntry($"{Path.GetRelativePath(rootPath, path)}{Path.DirectorySeparatorChar}");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                entry.ExternalAttributes = UnixDirectoryPermissions;
            }
        }

        foreach (FileInfo file in directory.GetFiles())
        {
            ZipArchiveEntry entry = archive.CreateEntry(Path.GetRelativePath(rootPath, file.FullName), Level);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                entry.ExternalAttributes = UnixFilePermissions;
            }

            using FileStream fileStream = File.Open(file.FullName, FileMode.Open);
            using Stream entryStream = entry.Open();
            fileStream.CopyTo(entryStream);
        }

        foreach (DirectoryInfo subDirectory in directory.GetDirectories())
        {
            AddPathToArchive(archive, rootPath, subDirectory.FullName);
        }
    }
}
