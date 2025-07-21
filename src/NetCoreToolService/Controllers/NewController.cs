// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Utils.Diagnostics;
using Steeltoe.Common.Utils.IO;
using Steeltoe.NetCoreToolService.Models;
using Steeltoe.NetCoreToolService.Packagers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.NetCoreToolService.Controllers
{
    /// <summary>
    /// The controller for "dotnet new".
    /// </summary>
    [ApiController]
    [Route("api/new")]
    public class NewController : ControllerBase
    {
        private const string DefaultOutput = "Sample";

        private const string DefaultPackaging = "zip";

        private readonly ICommandExecutor _commandExecutor;

        private readonly ILogger<NewController> _logger;

        private readonly Dictionary<string, IPackager> _packagers = new ()
        {
            { "zip", new ZipPackager() },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="NewController"/> class.
        /// </summary>
        /// <param name="commandExecutor">Injected command.</param>
        /// <param name="logger">Injected logger.</param>
        public NewController(ICommandExecutor commandExecutor, ILogger<NewController> logger = null)
        {
            _commandExecutor = commandExecutor;
            _logger = logger;
        }

        /// <summary>
        /// Gets the available Net Core Tool templates.
        /// </summary>
        /// <returns>Templates.</returns>
        [HttpGet]
        public async Task<ActionResult> GetTemplates()
        {
            return Ok(await GetTemplateDictionary());
        }

        /// <summary>
        /// Installs the Net Core Tool templates for the specified NuGet ID.
        /// </summary>
        /// <param name="nuGetId">Template NuGet ID.</param>
        /// <returns>Information about the installed templates.</returns>
        [HttpPut("nuget/{nuGetId}")]
        public async Task<ActionResult> InstallTemplates(string nuGetId)
        {
            await _commandExecutor.ExecuteAsync($"{NetCoreTool.Command} new uninstall {nuGetId}");
            var oldTemplates = await GetTemplateDictionary();
            var installCommand = await _commandExecutor.ExecuteAsync($"{NetCoreTool.Command} new install {nuGetId}");
            const string notFoundError = "error NU1101: ";
            if (installCommand.Output.Contains(notFoundError))
            {
                var start = installCommand.Output.IndexOf(notFoundError, StringComparison.Ordinal) +
                            notFoundError.Length;
                var end = installCommand.Output.IndexOf('\n', start);
                return BadRequest(installCommand.Output[start..end].Trim());
            }

            var newTemplates = await GetTemplateDictionary();
            foreach (var oldTemplate in oldTemplates.Keys)
            {
                newTemplates.Remove(oldTemplate);
            }

            return CreatedAtAction(nameof(InstallTemplates), newTemplates);
        }

        /// <summary>
        /// Uninstalls the Net Core Tool templates for the specified NuGet ID.
        /// </summary>
        /// <param name="nuGetId">Template NuGet ID.</param>
        /// <returns>Information about the installed templates.</returns>
        [HttpDelete("nuget/{nuGetId}")]
        public async Task<ActionResult> UninstallTemplates(string nuGetId)
        {
            var oldTemplates = await GetTemplateDictionary();
            var uninstallCommand =
                await _commandExecutor.ExecuteAsync($"{NetCoreTool.Command} new uninstall {nuGetId}");
            if (uninstallCommand.Output.Contains($"Could not find something to uninstall"))
            {
                return NotFound($"No templates with NuGet ID '{nuGetId}' installed.");
            }

            var newTemplates = await GetTemplateDictionary();
            foreach (var newTemplate in newTemplates.Keys)
            {
                oldTemplates.Remove(newTemplate);
            }

            return Ok(oldTemplates);
        }

        /// <summary>
        /// Returns "help" for the specified Net Core Tool template.
        /// </summary>
        /// <param name="template">Template name.</param>
        /// <returns>Template help.</returns>
        [HttpGet("{template}/help")]
        public async Task<ActionResult> GetTemplateHelp(string template)
        {
            var helpCommand = await _commandExecutor.ExecuteAsync($"{NetCoreTool.Command} new {template} --help");
            if (helpCommand.ExitCode != 0)
            {
                var start = helpCommand.Error.IndexOf("No templates found", StringComparison.Ordinal);
                var end = helpCommand.Error.IndexOf('\n', start);
                return NotFound(helpCommand.Error[start..end].Trim());
            }

            return Ok(helpCommand.Output.Trim());
        }

        /// <summary>
        /// Gets a generated project for the specified Net Core Tool template.
        /// </summary>
        /// <param name="template">Template name.</param>
        /// <param name="options">Template options.</param>
        /// <param name="packaging">Project packaging, e.g. zip.</param>
        /// <returns>Project archive.</returns>
        [HttpGet]
        [Route("{template}")]
        public async Task<ActionResult> GetTemplateProject(
            string template,
            string options = null,
            string packaging = DefaultPackaging)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _logger?.LogInformation("New: template={Template}, options={Options}, packaging={Packaging}", template, options, packaging);

            try
            {
                var output = DefaultOutput;
                var optionList = new List<string>();
                if (options is not null)
                {
                    foreach (var option in options.Split(','))
                    {
                        if (option.Contains('='))
                        {
                            var nvp = option.Split('=', 2);
                            if (nvp[0].Equals("output"))
                            {
                                output = nvp[1];
                                continue;
                            }

                            if (nvp[1].Contains(' '))
                            {
                                optionList.Add($"--{nvp[0]}=\"{nvp[1]}\"");
                                continue;
                            }
                        }

                        optionList.Add($"--{option}");
                    }
                }

                if (!_packagers.TryGetValue(packaging, out var packager))
                {
                    return BadRequest($"Unknown or unsupported packaging '{packaging}'.");
                }

                using var projectDir = new TempDirectory("NetCoreToolService-");
                var commandLine = new StringBuilder();
                commandLine.Append(NetCoreTool.Command).Append(" new ").Append(template);
                commandLine.Append(" --output=").Append(output);
                foreach (var option in optionList)
                {
                    commandLine.Append(' ').Append(option);
                }

                var newCommand =
                    await _commandExecutor.ExecuteAsync(commandLine.ToString(), projectDir.FullPath);

                const string unknownTemplateError = "No templates found";
                if (newCommand.Error.Contains(unknownTemplateError))
                {
                    return NotFound($"Template '{template}' not found.");
                }

                const string invalidOptionError = "Invalid option(s)";
                if (newCommand.Error.Contains(invalidOptionError))
                {
                    var start = newCommand.Error.IndexOf(invalidOptionError, StringComparison.Ordinal) +
                                invalidOptionError.Length;
                    start = newCommand.Error.IndexOf("--", start, StringComparison.Ordinal) + "--".Length;
                    var end = newCommand.Error.IndexOf(Environment.NewLine, start, StringComparison.Ordinal);
                    return NotFound($"Switch '{newCommand.Error[start..end]}' not found.");
                }

                const string invalidSwitchError = "Invalid input switch:";
                if (newCommand.Error.Contains(invalidSwitchError))
                {
                    var start = newCommand.Error.IndexOf(invalidSwitchError, StringComparison.Ordinal) +
                                invalidSwitchError.Length;
                    start = newCommand.Error.IndexOf("--", start, StringComparison.Ordinal) + "--".Length;
                    var end = newCommand.Error.IndexOf(Environment.NewLine, start, StringComparison.Ordinal);
                    return NotFound($"Switch '{newCommand.Error[start..end]}' not found.");
                }

                const string invalidParameterError = "Error: Invalid parameter(s):";
                if (newCommand.Error.Contains(invalidParameterError))
                {
                    var start = newCommand.Error.IndexOf(invalidParameterError, StringComparison.Ordinal) +
                                invalidParameterError.Length;
                    start = newCommand.Error.IndexOf("--", start, StringComparison.Ordinal) + "--".Length;
                    var end = newCommand.Error.IndexOf(Environment.NewLine, start, StringComparison.Ordinal);
                    var nvp = newCommand.Error[start..end].Split(' ', 2);
                    return NotFound($"Option '{nvp[0]}' parameter '{nvp[1]}' not found.");
                }

                if (newCommand.ExitCode != 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, newCommand.Error.Trim());
                }

                if (!newCommand.Output.Contains(" was created successfully."))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, newCommand.Output.Trim());
                }

                var package = packager.ToBytes(projectDir.FullPath);
                return File(package, packager.MimeType, $"{output}{packager.FileExtension}");
            }
            finally
            {
                stopwatch.Stop();
                _logger?.LogDebug("Generated project in {Elapsed:m\\:s\\.fff}", stopwatch.Elapsed);
            }
        }

        private async Task<TemplateDictionary> GetTemplateDictionary()
        {
            var listCommand = await _commandExecutor.ExecuteAsync($"{NetCoreTool.Command} new list");

            var lines = listCommand.Output.Split('\n').ToList()
                .FindAll(line => !string.IsNullOrWhiteSpace(line));

            var headingIdx = lines.FindIndex(line => line.StartsWith("-"));
            var headings = lines[headingIdx].Split("  ");
            const int nameColStart = 0;
            var nameColEnd = nameColStart + headings[0].Length;
            var shortNameColStart = nameColEnd + 2;
            var shortNameColEnd = shortNameColStart + headings[1].Length;
            var languageColStart = shortNameColEnd + 2;
            var languageColEnd = languageColStart + headings[2].Length;
            var tagsColStart = languageColEnd + 2;
            var tagsColEnd = tagsColStart + headings[3].Length;
            lines = lines.GetRange(headingIdx + 1, lines.Count - headingIdx - 1);

            var dict = new TemplateDictionary();
            foreach (var line in lines)
            {
                var template = line[shortNameColStart..shortNameColEnd].Trim();
                var templateInfo = new TemplateInfo
                {
                    Name = line[nameColStart..nameColEnd].Trim(),
                    Languages = line[languageColStart..languageColEnd].Trim(),
                    Tags = line[tagsColStart.. Math.Min(tagsColEnd, line.Length)].Trim(),
                };
                dict.Add(template, templateInfo);
            }

            return dict;
        }
    }
}
