// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.NetCoreToolService.Services
{
    /// <summary>
    /// Contract for archiver registry implementations.
    /// </summary>
    public interface IArchiverRegistry
    {
        /// <summary>
        /// Registers the archiver.
        /// </summary>
        /// <param name="value">The archiver to register.</param>
        void Register(IArchiver value);

        /// <summary>
        /// Look for an archiver with the specified name.
        /// </summary>
        /// <param name="name">The name of the archiver to loookup.</param>
        /// <returns>The named archiver,or <c>null</c> if no value found.</returns>
        IArchiver Lookup(string name);
    }
}
