// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Steeltoe.Logging.DynamicConsole;
using Steeltoe.NetCoreToolService.Models;
using System.Reflection;

namespace Steeltoe.NetCoreToolService
{
    /// <summary>
    /// The Steeltoe Net Core Tool Service program.
    /// </summary>
    public class Program
    {
        static Program()
        {
            var versionAttr =
                typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var fields = versionAttr?.InformationalVersion.Split('+') ?? ["unknown"];

            if (fields.Length == 1)
            {
                fields = [fields[0], "unknown"];
            }

            About = new About
            {
                Name = typeof(Program).Namespace ?? "unknown",
                Version = fields[0],
                Commit = fields[1],
            };
        }

        /// <summary>
        /// Gets or sets "About" details, such as version.
        /// </summary>
        public static About About { get; set; }

        /// <summary>
        /// Program entrypoint.
        /// </summary>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Create a host.
        /// </summary>
        /// <param name="args">Command line args.</param>
        /// <returns>A host.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((_, builder) => builder.AddDynamicConsole())
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
