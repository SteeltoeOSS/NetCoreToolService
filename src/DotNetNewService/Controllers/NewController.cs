using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Steeltoe.DotNetNewService.Models;
using Steeltoe.DotNetNewService.Utils;

namespace Steeltoe.DotNetNewService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewController : ControllerBase
    {
        private readonly ILogger<NewController> _logger;

        public NewController(ILogger<NewController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] NewSpec newSpec)
        {
            if (newSpec.Template is null)
            {
                return BadRequest("missing template");
            }

            newSpec.Name ??= "Sample";

            using var workDir = new TempDirectory();

            var args = new List<string>() { "new", newSpec.Template, "--output", newSpec.Name };
            if (!(newSpec.Options is null))
            {
                args.AddRange(newSpec.Options.Split(",").Select(option => $"--{option.Trim()}"));
            }

            var pInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = string.Join(' ', args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workDir.FullName,
            };

            var result = await ProcessToResult(pInfo);
            var ok = result as ContentResult;
            if (ok is null)
            {
                return result;
            }

            _logger.LogInformation("OKAY!!!!!!");

            var zipFile = $"{workDir.FullName}.zip";
            ZipFile.CreateFromDirectory(workDir.FullName, zipFile);

            var bytes = await System.IO.File.ReadAllBytesAsync(zipFile);
            System.IO.File.Delete(zipFile);
            return File(bytes, "application/zip", $"{newSpec.Name}.zip");
        }

        [HttpGet]
        [Route("templates")]
        public async Task<ActionResult> GetTemplates()
        {
            using var workDir = new TempDirectory();

            var pInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { "new", "--list" },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workDir.FullName,
            };

            return await ProcessToResult(pInfo);
        }

        [HttpGet]
        [Route("templates/{id}")]
        public async Task<ActionResult> GetTemplate(string id)
        {
            using var workDir = new TempDirectory();

            var pInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { "new", id, "--help" },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workDir.FullName,
            };

            return await ProcessToResult(pInfo);
        }

        [HttpPost]
        [Route("templates")]
        public async Task<ActionResult> PostTemplate(TemplateSpec templateSpec)
        {
            if (templateSpec.NuGetId is null)
            {
                return BadRequest("missing NuGet ID");
            }

            var pInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList = { "new", "--install", templateSpec.NuGetId },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            return await ProcessToResult(pInfo);
        }

        private async Task<ActionResult> ProcessToResult(ProcessStartInfo processStartInfo)
        {
            var guid = Path.GetFileName(processStartInfo.WorkingDirectory) ?? "unknown";
            _logger.LogInformation("{Guid}: {Command} {Args}", guid, processStartInfo.FileName,
                processStartInfo.Arguments);
            var proc = Process.Start(processStartInfo);
            if (proc is null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            await proc.WaitForExitAsync();
            if (proc.ExitCode != 0)
            {
                var error = await proc.StandardError.ReadToEndAsync();
                _logger.LogInformation("{Guid}: {Error}", guid, error);
                return NotFound(error);
            }

            return Content(await proc.StandardOutput.ReadToEndAsync());
        }
    }
}
