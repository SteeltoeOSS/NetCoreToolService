// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace Steeltoe.NetCoreToolService.SteeltoeUtils.Diagnostics;

/// <inheritdoc />
public sealed partial class CommandExecutor : ICommandExecutor
{
    private static int _commandCounter;
    private readonly ILogger<CommandExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutor" /> class.
    /// </summary>
    /// <param name="logger">
    /// Injected logger.
    /// </param>
    public CommandExecutor(ILogger<CommandExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CommandResult> ExecuteAsync(string command, string? workingDirectory, int timeout)
    {
        ArgumentNullException.ThrowIfNull(command);

        int commandId = NextCommandId();
        using Process process = CreateProcess(command, workingDirectory);

        var output = new StringBuilder();
        var outputCloseEvent = new TaskCompletionSource<bool>();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                outputCloseEvent.SetResult(true);
            }
            else
            {
                output.AppendLine(e.Data);
            }
        };

        var error = new StringBuilder();
        var errorCloseEvent = new TaskCompletionSource<bool>();

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                errorCloseEvent.SetResult(true);
            }
            else
            {
                error.AppendLine(e.Data);
            }
        };

        LogCommand(commandId, command);

        try
        {
            if (!process.Start())
            {
                LogStartFailed(commandId, "no details available");
                throw new CommandException($"'{command}' failed to start; no details available");
            }
        }
        catch (Exception exception) when (exception is not CommandException)
        {
            LogStartThrown(commandId, exception.Message);
            throw new CommandException($"'{command}' failed to start: {exception.Message}", exception);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // ReSharper disable once AccessToDisposedClosure
        Task<bool> waitForExit = Task.Run(() => process.WaitForExit(timeout));
        Task<bool[]> processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

        if (await Task.WhenAny(Task.Delay(timeout), processTask) == processTask && waitForExit.Result)
        {
            var result = new CommandResult
            {
                ExitCode = process.ExitCode,
                Output = output.ToString(),
                Error = error.ToString()
            };

            LogExitCode(commandId, result.ExitCode);

            if (result.Output.Length > 0)
            {
                LogStandardOutput(commandId, result.Output);
            }

            if (result.Error.Length > 0)
            {
                LogStandardError(commandId, result.Error);
            }

            return result;
        }

        try
        {
            process.Kill();
        }
        catch
        {
            // ignore
        }

        LogTimedOut(commandId, timeout);
        throw new CommandException($"'{process.StartInfo.FileName} {process.StartInfo.Arguments}' timed out");
    }

    private static int NextCommandId()
    {
        return Interlocked.Increment(ref _commandCounter);
    }

    private static Process CreateProcess(string command, string? workingDirectory)
    {
        Process? process = null;

        try
        {
            process = new Process();
            string[] arguments = command.Split([' '], 2);
            process.StartInfo.FileName = arguments[0];

            if (arguments.Length > 1)
            {
                process.StartInfo.Arguments = arguments[1];
            }

            if (workingDirectory != null)
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            return process;
        }
        catch
        {
            process?.Dispose();
            throw;
        }
    }

    [LoggerMessage(LogLevel.Debug, "[{CommandId}] command: {Command}")]
    partial void LogCommand(int commandId, string command);

    [LoggerMessage(LogLevel.Debug, "[{CommandId}] failed to start: {Error}")]
    partial void LogStartFailed(int commandId, string error);

    [LoggerMessage(LogLevel.Debug, "[{CommandId}] failed to start: {Error}")]
    partial void LogStartThrown(int commandId, string error);

    [LoggerMessage(LogLevel.Debug, "[{CommandId}] exit code: {ExitCode}")]
    partial void LogExitCode(int commandId, int exitCode);

    [LoggerMessage(LogLevel.Debug, "[{CommandId}] stdout:\n{Output}")]
    partial void LogStandardOutput(int commandId, string output);

    [LoggerMessage(LogLevel.Debug, "[{CommandId}] stderr:\n{Error}")]
    partial void LogStandardError(int commandId, string error);

    [LoggerMessage(LogLevel.Debug, "[{CommandId}] timed out: {TimeOut}ms")]
    partial void LogTimedOut(int commandId, int timeOut);
}
