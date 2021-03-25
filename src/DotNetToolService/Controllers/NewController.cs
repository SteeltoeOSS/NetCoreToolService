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
            var pInfo = new ProcessStartInfo
            {
                ArgumentList = { "new", "--list" },
            };

            return await ProcessToResult(pInfo);
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

            var result = await ProcessToResult(pInfo);
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
        [Route("{id}/help")]
        public async Task<ActionResult> GetTemplate(string id)
        {
            var pInfo = new ProcessStartInfo
            {
                ArgumentList = { "new", id, "--help" },
            };

            return await ProcessToResult(pInfo);
        }

        [HttpPost]
        public async Task<ActionResult> PostTemplate(string nuGetId)
        {
            if (nuGetId is null)
            {
                return BadRequest("missing NuGet ID");
            }

            var pInfo = new ProcessStartInfo
            {
                ArgumentList = { "new", "--install", nuGetId },
            };

            return await ProcessToResult(pInfo);
        }

        private async Task<ActionResult> ProcessToResult(ProcessStartInfo processStartInfo)
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
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            await proc.WaitForExitAsync();
            workDir?.Dispose();
            if (proc.ExitCode == 0)
            {
                return Content(await proc.StandardOutput.ReadToEndAsync());
            }

            var error = await proc.StandardError.ReadToEndAsync();
            _logger.LogInformation("{Guid}: {Error}", guid, error);
            return NotFound(error);
        }
    }
}
