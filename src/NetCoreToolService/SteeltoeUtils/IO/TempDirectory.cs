// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.NetCoreToolService.SteeltoeUtils.IO;

/// <summary>
/// A temporary directory.
/// </summary>
public sealed class TempDirectory(string? prefix)
    : TempPath(prefix)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TempDirectory" /> class.
    /// </summary>
    public TempDirectory()
        : this(null)
    {
    }

    /// <summary>
    /// Creates the temporary directory.
    /// </summary>
    protected override void InitializePath()
    {
        Directory.CreateDirectory(FullPath);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!Directory.Exists(FullPath))
        {
            return;
        }

        try
        {
            Directory.Delete(FullPath, true);
        }
        catch
        {
            // ignore
        }
    }
}
