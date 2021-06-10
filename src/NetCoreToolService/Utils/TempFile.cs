// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Steeltoe.NetCoreToolService.Utils
{
    /// <summary>
    /// A temp file.
    /// </summary>
    public sealed class TempFile : TempPath
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempFile"/> class.
        /// </summary>
        public TempFile()
            : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempFile"/> class.
        /// </summary>
        /// <param name="create">If true, create file.</param>
        public TempFile(bool create)
        {
            if (create)
            {
                File.Create(FullName).Dispose();
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!File.Exists(FullName))
            {
                return;
            }

            try
            {
                File.Delete(FullName);
            }
            catch
            {
                // ignore
            }
        }
    }
}
