// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Steeltoe.Logging.DynamicConsole;
using Steeltoe.Management.Endpoint.Actuators.All;
using Steeltoe.NetCoreToolService.Models;
using Steeltoe.NetCoreToolService.SteeltoeUtils.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.AddDynamicConsole();
builder.Services.AddAllActuators();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddTransient<ICommandExecutor, CommandExecutor>();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<About>>();
About.LogCurrent(logger);

await app.RunAsync();
