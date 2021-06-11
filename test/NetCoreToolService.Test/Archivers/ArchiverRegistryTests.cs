// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.NetCoreToolService.Archivers;
using Xunit;

namespace Steeltoe.NetCoreToolService.Test.Archivers
{
    public class ArchiverRegistryTests
    {
        /* ----------------------------------------------------------------- *
         * positive tests                                                    *
         * ----------------------------------------------------------------- */

        [Fact]
        public void Zip_Format_Should_Return_ZipArchiver()
        {
            // Arrange
            var registry = new ArchiverRegistry(new NullLogger<ArchiverRegistry>());

            // Act
            var archiver = registry.Lookup("zip");

            // Assert
            archiver.Should().NotBeNull();
            archiver.Should().BeOfType<ZipArchiver>();
        }

        /* ----------------------------------------------------------------- *
         * negative tests                                                    *
         * ----------------------------------------------------------------- */

        [Fact]
        public void Unknown_Format_Should_Return_Null()
        {
            // Arrange
            var registry = new ArchiverRegistry(new NullLogger<ArchiverRegistry>());

            // Act
            var archiver = registry.Lookup("unknown");

            // Assert
            archiver.Should().BeNull();
        }
    }
}
