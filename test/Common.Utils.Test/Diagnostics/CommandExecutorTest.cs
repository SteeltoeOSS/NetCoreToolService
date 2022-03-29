// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Steeltoe.Common.Utils.Diagnostics;
using Xunit;

namespace Steeltoe.Common.Utils.Test.Diagnostics
{
    public class CommandExecutorTest
    {
        private readonly CommandExecutor _commandExecutor = new();

        [Fact]
        public async void SuccessfulCommandShouldReturn0()
        {
            var result = await _commandExecutor.ExecuteAsync("dotnet --help");
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Usage: dotnet", result.Output);
        }

        [Fact]
        public async void UnsuccessfulCommandShouldNotReturn0()
        {
            var result = await _commandExecutor.ExecuteAsync("dotnet --no-such-option");
            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("--no-such-option does not exist", result.Error);
        }

        [Fact]
        public async void UnknownCommandShouldThrowException()
        {
            Task Act() => _commandExecutor.ExecuteAsync("no-such-command");
            var exc = await Assert.ThrowsAsync<CommandException>(Act);
            Assert.Contains("'no-such-command' failed to start", exc.Message);
            Assert.NotNull(exc.InnerException);
        }
    }
}
