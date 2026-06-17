// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.NetCoreToolService.Models;

/// <summary>
/// Application information, such as version.
/// </summary>
/// <param name="Name">
/// The application name.
/// </param>
/// <param name="Version">
/// The application version.
/// </param>
/// <param name="Commit">
/// The application build source control commit ID.
/// </param>
internal readonly record struct About(string Name, string Version, string Commit)
{
    public static void LogCurrent(ILogger<About> logger)
    {
        About about = GetCurrent();
        AboutLogger.LogVersion(logger, about.Name, about.Version, about.Commit);
    }

    private static About GetCurrent()
    {
        var versionAttribute = typeof(About).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        string[] fields = versionAttribute?.InformationalVersion.Split('+') ?? ["unknown"];

        fields = fields.Length switch
        {
            1 =>
            [
                fields[0],
                "unknown"
            ],
            _ => fields
        };

        return new About
        {
            Name = typeof(About).Namespace ?? "unknown",
            Version = fields[0],
            Commit = fields[1]
        };
    }
}
