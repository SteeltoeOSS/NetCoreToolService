// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.NetCoreToolService.SteeltoeUtils.Diagnostics;

/// <summary>
/// A utility abstraction to simplify the running of commands.
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Executes the command and returns the result.
    /// </summary>
    /// <param name="command">
    /// Command to be executed.
    /// </param>
    /// <param name="workingDirectory">
    /// The directory that contains the command process.
    /// </param>
    /// <param name="timeout">
    /// The amount of time in milliseconds to wait for the command to complete.
    /// </param>
    /// <returns>
    /// The command result.
    /// </returns>
    /// <exception cref="CommandException">
    /// Thrown if a process can not be started for the specified command.
    /// </exception>
    Task<CommandResult> ExecuteAsync(string command, string? workingDirectory, int timeout);
}
