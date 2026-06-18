// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using FluentAssertions;
using Steeltoe.NetCoreToolService.Packagers;
using Steeltoe.NetCoreToolService.SteeltoeUtils.IO;
using Xunit;

namespace Steeltoe.NetCoreToolService.Test.Packagers;

public sealed class ZipPackagerTests
{
    [Fact]
    public void ToStream_Should_Create_Zip_Archive()
    {
        // Arrange
        var packager = new ZipPackager();
        using var tempDir = new TempDirectory();

        // Act
        byte[] buffer = packager.ToBytes(tempDir.FullPath);

        // Assert
        using var stream = new MemoryStream(buffer);
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
        zipArchive.Entries.Should().BeEmpty();
    }

    [Fact]
    public void ToStream_Should_Archive_File_Contents()
    {
        // Arrange
        var packager = new ZipPackager();
        using var tempDir = new TempDirectory();
        string dir1 = Path.Join(tempDir.FullPath, "d1");
        Directory.CreateDirectory(dir1);
        string file1 = Path.Join(dir1, "f1");
        File.WriteAllText(file1, "content1");

        // Act
        byte[] buffer = packager.ToBytes(tempDir.FullPath);

        // Assert
        using var stream = new MemoryStream(buffer);
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);

        zipArchive.Entries.Should().HaveCount(2);
        zipArchive.Entries[0].FullName.Should().Be("d1/");
        zipArchive.Entries[1].FullName.Should().Be("d1/f1");

        using var reader = new StreamReader(zipArchive.Entries[1].Open());
        reader.ReadToEnd().Should().Be("content1");
    }

    [Fact]
    public void ToStream_Should_Archive_Directories()
    {
        // Arrange
        var packager = new ZipPackager();
        using var tempDir = new TempDirectory();
        string dir1 = Path.Join(tempDir.FullPath, "d1");
        Directory.CreateDirectory(dir1);
        string dir2 = Path.Join(dir1, "d2");
        Directory.CreateDirectory(dir2);

        // Act
        byte[] buffer = packager.ToBytes(tempDir.FullPath);

        // Assert
        using var stream = new MemoryStream(buffer);
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);

        zipArchive.Entries.Should().HaveCount(2);
        zipArchive.Entries[0].FullName.Should().Be("d1/");
        zipArchive.Entries[1].FullName.Should().Be("d1/d2/");

        using var reader = new StreamReader(zipArchive.Entries[0].Open());
        reader.ReadToEnd().Should().BeEmpty();
    }

    [Fact]
    public void Name_Should_Be_zip()
    {
        // Arrange
        var packager = new ZipPackager();

        // Act
        string name = packager.Name;

        // Assert
        name.Should().Be("zip");
    }

    [Fact]
    public void FileExtension_Should_Be_zip()
    {
        // Arrange
        var packager = new ZipPackager();

        // Act
        string extension = packager.FileExtension;

        // Assert
        extension.Should().Be(".zip");
    }

    [Fact]
    public void MimeType_Should_Be_application_zip()
    {
        // Arrange
        var packager = new ZipPackager();

        // Act
        string mimeType = packager.MimeType;

        // Assert
        mimeType.Should().Be("application/zip");
    }
}
