// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
internal readonly record struct About(string Name, string Version, string Commit);
