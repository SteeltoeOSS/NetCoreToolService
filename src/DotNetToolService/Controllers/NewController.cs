using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Steeltoe.DotNetToolService.Utils;

namespace Steeltoe.DotNetToolService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewController : ControllerBase
    {
        private readonly ILogger<NewController> _logger;

        private static readonly string Dotnet = "dotnet";

        public NewController(ILogger<NewController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetTemplates()
        {
            return Ok(await GetTemplateDictionary());
        }

        [HttpGet]
        [Route("{template}")]
        public async Task<ActionResult> GetProject(string template, string options)
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

            using var zipFile = new TempFile(false);
            ZipFile.CreateFromDirectory(workDir.FullName, zipFile.FullName);

            var bytes = await System.IO.File.ReadAllBytesAsync(zipFile.FullName);
            return File(bytes, "application/zip", $"{name}.zip");
        }

        [HttpGet]
        [Route("{template}/help")]
        public async Task<ActionResult> GetTemplate(string template)
        {
            var pInfo = new ProcessStartInfo
            {
                ArgumentList = { "new", template, "--help" },
            };

            return await ProcessToResultAsync(pInfo);
        }

        [HttpPost]
        public async Task<ActionResult> PostTemplate(string nuGetId)
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
            var headings = lines[1].Split("  ");
            var nameColStart = 0;
            var nameColLength = headings[0].Length;
            var shortNameColStart = nameColStart + nameColLength + 2;
            var shortNameColLength = headings[1].Length;
            var languageColStart = shortNameColStart + shortNameColLength + 2;
            var languageColLength = headings[2].Length;
            var tagsColStart = languageColStart + languageColLength + 2;
            var tagsColLength = headings[3].Length;
            lines = lines.GetRange(2, lines.Count - 2);
            lines = lines.GetRange(2, lines.Count - 2);

            var dict = new Dictionary<string, TemplateInfo>();
            foreach (var line in lines)
            {
                var templateInfo = new TemplateInfo();
                var template = line.Substring(shortNameColStart, shortNameColLength).Trim();
                templateInfo.Name = line.Substring(nameColStart, nameColLength).Trim();
                templateInfo.Languages = line.Substring(languageColStart, languageColLength).Trim();
                templateInfo.Tags = line.Substring(tagsColStart, tagsColLength).Trim();
                dict.Add(template, templateInfo);
            }

            return dict;
        }

        private async Task<string> ProcessToStringAsync(ProcessStartInfo processStartInfo)
        {
            processStartInfo.FileName = Dotnet;
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
            _logger.LogInformation("{Guid}: {Command} {Args}", guid, processStartInfo.FileName,
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

    class TemplateInfo
    {
        public string Name { get; set; }

        public string Languages { get; set; }

        public string Tags { get; set; }
    }

    class ActionResultException : Exception
    {
        internal ActionResult ActionResult { get; }

        internal ActionResultException(ActionResult actionResult)
        {
            ActionResult = actionResult;
        }
    }
}
