// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.NetCoreToolService.Services
{
    /// <summary>
    /// Contract for archiver implementations.
    /// </summary>
    public interface IArchiver
    {
        /// <summary>
        /// Gets the name for the archiver, e.g. <c>"zip"</c>.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the file extension for the archive, e.g. <c>".zip"</c>.
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// Gets the mime type for the archive, e.g. <c>"application/zip"</c>.
        /// </summary>
        string MimeType { get; }

        /// <summary>
        /// Returns an archive of the files rooted at <c>path</c> as a byte array.
        /// </summary>
        /// <param name="path">Path to be archived.</param>
        /// <returns>A byte array containing the archive.</returns>
        byte[] ToBytes(string path);
    }
}
