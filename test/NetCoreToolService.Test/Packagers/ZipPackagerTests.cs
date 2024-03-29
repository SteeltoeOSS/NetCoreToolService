// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.IO.Compression;
using FluentAssertions;
using Steeltoe.Common.Utils.IO;
using Steeltoe.NetCoreToolService.Packagers;
using Xunit;

namespace Steeltoe.NetCoreToolService.Test.Packagers
{
    public class ZipPackagerTests
    {
        /* ----------------------------------------------------------------- *
         * positive tests                                                    *
         * ----------------------------------------------------------------- */

        [Fact]
        public void ToStream_Should_Create_Zip_Archive()
        {
            // Arrange
            var archiver = new ZipPackager();
            var tempDir = new TempDirectory();

            // Act
            var buf = archiver.ToBytes(tempDir.FullPath);

            // Assert
            new ZipArchive(new MemoryStream(buf)).Should().BeOfType<ZipArchive>();
        }

        [Fact]
        public void ToStream_Should_Archive_File_Contents()
        {
            // Arrange
            var archiver = new ZipPackager();
            using var tempDir = new TempDirectory();
            var d1 = Path.Join(tempDir.FullPath, "d1");
            Directory.CreateDirectory(d1);
            var f1 = Path.Join(d1, "f1");
            File.WriteAllText(f1, "f1 stuff");

            // Act
            var buf = archiver.ToBytes(tempDir.FullPath);

            // Assert
            var zip = new ZipArchive(new MemoryStream(buf));
            using var entries = zip.Entries.GetEnumerator();
            entries.MoveNext().Should().BeTrue();
            Assert.NotNull(entries.Current);
            entries.Current.FullName.Should().Be($"d1{Path.DirectorySeparatorChar}");
            entries.MoveNext().Should().BeTrue();
            Assert.NotNull(entries.Current);
            entries.Current.Name.Should().Be("f1");
            entries.Current.FullName.Should().Be($"d1{Path.DirectorySeparatorChar}f1");
            using var reader = new StreamReader(entries.Current.Open());
            reader.ReadToEnd().Should().Be("f1 stuff");
            entries.MoveNext().Should().BeFalse();
        }

        [Fact]
        public void ToStream_Should_Archive_Directories()
        {
            // Arrange
            var archiver = new ZipPackager();
            using var tempDir = new TempDirectory();
            var d1 = Path.Join(tempDir.FullPath, "d1");
            Directory.CreateDirectory(d1);
            var d2 = Path.Join(d1, "d2");
            Directory.CreateDirectory(d2);

            // Act
            var buf = archiver.ToBytes(tempDir.FullPath);

            // Assert
            var zip = new ZipArchive(new MemoryStream(buf));
            using var entries = zip.Entries.GetEnumerator();
            entries.MoveNext().Should().BeTrue();
            Assert.NotNull(entries.Current);
            entries.Current.FullName.Should().Be($"d1{Path.DirectorySeparatorChar}");
            using var reader = new StreamReader(entries.Current.Open());
            reader.ReadToEnd().Should().BeEmpty();
            entries.MoveNext().Should().BeTrue();
            Assert.NotNull(entries.Current);
            entries.Current.FullName.Should().Be($"d1{Path.DirectorySeparatorChar}d2{Path.DirectorySeparatorChar}");
            entries.MoveNext().Should().BeFalse();
        }

        [Fact]
        public void GetPackaging_Should_Be_application_zip()
        {
            // Arrange
            var archiver = new ZipPackager();

            // Act
            var packaging = archiver.Name;

            // Assert
            packaging.Should().Be("zip");
        }

        [Fact]
        public void GetFileExtension_Should_Be_zip()
        {
            // Arrange
            var archiver = new ZipPackager();

            // Act
            var ext = archiver.FileExtension;

            // Assert
            ext.Should().Be(".zip");
        }

        [Fact]
        public void MimeType_Should_Be_application_zip()
        {
            // Arrange
            var archiver = new ZipPackager();

            // Act
            var ext = archiver.MimeType;

            // Assert
            ext.Should().Be("application/zip");
        }

        /* ----------------------------------------------------------------- *
         * negative tests                                                    *
         * ----------------------------------------------------------------- */
    }
}
