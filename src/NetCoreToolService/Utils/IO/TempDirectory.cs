// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Steeltoe.NetCoreToolService.Utils.IO
{
    /// <summary>
    /// A temp directory.
    /// </summary>
    public sealed class TempDirectory : TempPath
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempDirectory"/> class.
        /// </summary>
        public TempDirectory()
            : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempDirectory"/> class.
        /// </summary>
        /// <param name="create">If true, create the directory.</param>
        public TempDirectory(bool create)
        {
            if (create)
            {
                Directory.CreateDirectory(FullName);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!Directory.Exists(FullName))
            {
                return;
            }

            try
            {
                Directory.Delete(FullName, true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
