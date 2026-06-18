// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.NetCoreToolService;

internal sealed class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IDisposable> AcquireAsync()
    {
        await _semaphore.WaitAsync();
        return new ReleaseOnDispose(this);
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    private sealed class ReleaseOnDispose(AsyncLock owner) : IDisposable
    {
        public void Dispose()
        {
            owner._semaphore.Release();
        }
    }
}
