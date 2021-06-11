// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;

namespace Steeltoe.NetCoreToolService.Utils.IO
{
    /// <summary>
    /// An abstraction of temp files.
    /// </summary>
    public abstract class TempPath : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempPath"/> class.
        /// </summary>
        protected TempPath()
        {
            Name = Guid.NewGuid().ToString();
            FullName = Path.Join(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, Name);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TempPath"/> class.
        /// </summary>
        ~TempPath()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the full name of this path.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets the name of this path.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Ensures the underlying temporary path is deleted.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Ensures the underlying temporary path is deleted.
        /// </summary>
        /// <param name="disposing">If disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}
