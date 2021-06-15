// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.NetCoreToolService.Packagers
{
    /// <summary>
    /// Contract for packager implementations.
    /// </summary>
    public interface IPackager
    {
        /// <summary>
        /// Gets the name for the packager, e.g. <c>"zip"</c>.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the file extension for package, e.g. <c>".zip"</c>.
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// Gets the mime type for a package, e.g. <c>"application/zip"</c>.
        /// </summary>
        string MimeType { get; }

        /// <summary>
        /// Returns a package of the files rooted at <c>path</c> as a byte array.
        /// </summary>
        /// <param name="path">Path to be packaged.</param>
        /// <returns>A byte array containing the package.</returns>
        byte[] ToBytes(string path);
    }
}
