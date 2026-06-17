// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.NetCoreToolService.SteeltoeUtils.Diagnostics;

/// <summary>
/// A simple representation of a command result.
/// </summary>
/// <param name="ExitCode">
/// The command exit code.
/// </param>
/// <param name="Output">
/// The command exit STDOUT.
/// </param>
/// <param name="Error">
/// The command exit STDERR.
/// </param>
public readonly record struct CommandResult(int ExitCode, string Output, string Error);
