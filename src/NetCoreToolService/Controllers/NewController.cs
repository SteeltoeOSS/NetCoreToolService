// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Steeltoe.NetCoreToolService.Services;
using Steeltoe.NetCoreToolService.Utils.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.NetCoreToolService.Controllers
{
    /// <summary>
    /// The controller for "dotnet new".
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NewController : ControllerBase
    {
        private readonly IArchiverRegistry _archiverRegistry;

        private readonly ILogger<NewController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewController"/> class.
        /// </summary>
        /// <param name="archiverRegistry">Injected registry of available archivers.</param>
        /// <param name="logger">Injected logger.</param>
        public NewController(IArchiverRegistry archiverRegistry, ILogger<NewController> logger)
        {
            _archiverRegistry = archiverRegistry;
            _logger = logger;
        }

        /// <summary>
        /// Gets the available Net Core Tool templates.
        /// </summary>
        /// <returns>Templates.</returns>
        [HttpGet]
        public async Task<ActionResult> GetTemplates()
        {
            var dict = await GetTemplateDictionary();
            return Ok(dict);
        }

        /// <summary>
        /// Gets a generated project for the specified Net Core Tool template.
        /// </summary>
        /// <param name="template">Template name.</param>
        /// <param name="options">Template options.</param>
        /// <returns>Project archive.</returns>
        [HttpGet]
        [Route("{template}")]
        public async Task<ActionResult> GetProjectArchiveForTemplate(string template, string options)
        {
            var opts = options?.Split(',').Select(opt => opt.Trim()).ToList() ?? new List<string>();
            var pArgs = new List<string>() { "new", template };
            var name = opts.Find(opt => opt.StartsWith("output="))?.Split('=', 2)[1];
            if (name is null)
            {
                name = "Sample";
                pArgs.AddRange(new[] { "--output", name });
            }

            pArgs.AddRange(opts.Select(opt => $"--{opt}"));

            using var workDir = new TempDirectory();
            var pInfo = new ProcessStartInfo
            {
                Arguments = string.Join(' ', pArgs),
                WorkingDirectory = workDir.FullName,
            };

            var result = await ProcessToResultAsync(pInfo);
            var ok = result as ContentResult;
            if (ok is null)
            {
                return result;
            }

            if (!Directory.EnumerateFileSystemEntries(workDir.FullName).Any())
            {
                return NotFound($"template {template} does not exist");
            }

            var archivalType = "zip";
            var archiver = _archiverRegistry.Lookup(archivalType);
            if (archiver is null)
            {
                return NotFound($"Packaging '{archivalType}' not found.");
            }

            var archiveBytes = archiver.ToBytes(workDir.FullName);
            return File(archiveBytes, archiver.MimeType, $"{name}{archiver.FileExtension}");
        }

        /// <summary>
        /// Returns "help" for the specified Net Core Tool template.
        /// </summary>
        /// <param name="template">Template name.</param>
        /// <returns>Template help.</returns>
        [HttpGet]
        [Route("{template}/help")]
        public async Task<ActionResult> GetTemplateHelp(string template)
        {
            var pInfo = new ProcessStartInfo
            {
                ArgumentList = { "new", template, "--help" },
            };

            return await ProcessToResultAsync(pInfo);
        }

        /// <summary>
        /// Installs the Net Core Tool templates for the specified NuGet ID.
        /// </summary>
        /// <param name="nuGetId">Template NuGet ID.</param>
        /// <returns>Information about the installed templates.</returns>
        [HttpPost]
        public async Task<ActionResult> InstallTemplate(string nuGetId)
        {
            if (nuGetId is null)
            {
                return BadRequest("missing NuGet ID");
            }

            var preInstallTemplates = await GetTemplateDictionary();

            var pInfo = new ProcessStartInfo
            {
                ArgumentList = { "new", "--install", nuGetId },
            };
            await ProcessToStringAsync(pInfo);

            var postInstallTemplates = await GetTemplateDictionary();

            foreach (var template in preInstallTemplates.Keys)
            {
                postInstallTemplates.Remove(template);
            }

            return Ok(postInstallTemplates);
        }

        private async Task<Dictionary<string, TemplateInfo>> GetTemplateDictionary()
        {
            var pInfo = new ProcessStartInfo
            {
                ArgumentList = { "new", "--list" },
            };
            var listing = await ProcessToStringAsync(pInfo);
            var lines = listing.Split('\n').ToList().FindAll(line => !string.IsNullOrWhiteSpace(line));
            var headingIdx = lines.FindIndex(line => line.StartsWith("-"));
            var headings = lines[headingIdx].Split("  ");
            var nameColLength = headings[0].Length;
            var shortNameColStart = nameColLength + 2;
            var shortNameColLength = headings[1].Length;
            var languageColStart = shortNameColStart + shortNameColLength + 2;
            var languageColLength = headings[2].Length;
            var tagsColStart = languageColStart + languageColLength + 2;
            var tagsColLength = headings[3].Length;
            lines = lines.GetRange(headingIdx + 1, lines.Count - headingIdx - 1);

            var dict = new Dictionary<string, TemplateInfo>();
            foreach (var line in lines)
            {
                var templateInfo = new TemplateInfo();
                var template = line.Substring(shortNameColStart, shortNameColLength).Trim();
                templateInfo.Name = line.Substring(0, nameColLength).Trim();
                templateInfo.Languages = line.Substring(languageColStart, languageColLength).Trim();
                templateInfo.Tags = line.Substring(tagsColStart, tagsColLength).Trim();
                dict.Add(template, templateInfo);
            }

            return dict;
        }

        private async Task<string> ProcessToStringAsync(ProcessStartInfo processStartInfo)
        {
            processStartInfo.FileName = NetCoreTool.Command;
            TempDirectory workDir = null;
            if (string.IsNullOrEmpty(processStartInfo.WorkingDirectory))
            {
                workDir = new TempDirectory();
                processStartInfo.WorkingDirectory = workDir.FullName;
            }

            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            var guid = Path.GetFileName(processStartInfo.WorkingDirectory) ?? "unknown";
            _logger.LogInformation(
                "{Guid}: {Command} {Args}",
                guid,
                processStartInfo.FileName,
                processStartInfo.Arguments);
            var proc = Process.Start(processStartInfo);
            if (proc is null)
            {
                throw new ActionResultException(StatusCode(StatusCodes.Status503ServiceUnavailable));
            }

            await proc.WaitForExitAsync();
            workDir?.Dispose();
            if (proc.ExitCode == 0)
            {
                var output = await proc.StandardOutput.ReadToEndAsync();
                _logger.LogInformation("{Guid}>\n{Output}", guid, output);
                return output;
            }

            var error = await proc.StandardError.ReadToEndAsync();
            _logger.LogInformation("{Guid}: {Error}", guid, error);
            throw new ActionResultException(NotFound(error));
        }

        private async Task<ActionResult> ProcessToResultAsync(ProcessStartInfo processStartInfo)
        {
            try
            {
                return Content(await ProcessToStringAsync(processStartInfo));
            }
            catch (ActionResultException e)
            {
                return e.ActionResult;
            }
        }
    }

    internal class TemplateInfo
    {
        public string Name { get; set; }

        public string Languages { get; set; }

        public string Tags { get; set; }

        public override string ToString()
        {
            return $"[name={Name},languages={Languages},tags={Tags}";
        }
    }

    internal class ActionResultException : Exception
    {
        internal ActionResultException(ActionResult actionResult)
        {
            ActionResult = actionResult;
        }

        internal ActionResult ActionResult { get; }
    }
}
