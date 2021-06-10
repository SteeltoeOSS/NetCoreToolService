// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.NetCoreToolService.Services;

namespace Steeltoe.NetCoreToolService
{
    /// <summary>
    /// The Steeltoe Net Core Tool Service program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Program entrypoint.
        /// </summary>
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            host.Services.GetRequiredService<IArchiverRegistry>().Initialize();
            host.Run();
        }

        /// <summary>
        /// Create a host.
        /// </summary>
        /// <param name="args">Command line args.</param>
        /// <returns>A host.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
