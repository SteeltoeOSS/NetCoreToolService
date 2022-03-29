// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Steeltoe.Common.Utils.Diagnostics;
using Steeltoe.Management.Endpoint;
using System.Text.Json.Serialization;

namespace Steeltoe.NetCoreToolService
{
    /// <summary>
    /// The Steeltoe Net Core Tool Service dependency injection setup.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">Injected configuration.</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Called by the runtime.
        /// </summary>
        /// <param name="services">Injected services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });
            services.AddTransient<ICommandExecutor, CommandExecutor>();
            services.AddAllActuators();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Steeltoe.NetCoreToolService", Version = "v0" });
            });
        }

        /// <summary>
        /// Called by the runtime.  Sets up HTTP request pipeline.
        /// </summary>
        /// <param name="app">Injected IApplicationBuilder.</param>
        /// <param name="env">Injected IWebHostEnvironment.</param>
        /// <param name="logger">Injected ILogger.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            var about = Program.About;
            logger.LogInformation("{Program}, version {Version} [{Commit}]", about.Name, about.Version, about.Commit);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NetCoreTool v0"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAllActuators();
                endpoints.MapControllers();
            });
        }
    }
}
