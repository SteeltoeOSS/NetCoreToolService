// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.NetCoreToolService.Models;

internal static partial class AboutLogger
{
    [LoggerMessage(LogLevel.Information, "{Program}, version {Version} [{Commit}]")]
    public static partial void LogVersion(ILogger logger, string program, string version, string commit);
}
