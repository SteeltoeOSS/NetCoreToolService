// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Steeltoe.NetCoreToolService.Models;
using Steeltoe.NetCoreToolService.Packagers;
using Steeltoe.NetCoreToolService.SteeltoeUtils.Diagnostics;
using Steeltoe.NetCoreToolService.SteeltoeUtils.IO;

#pragma warning disable S2360 // Optional parameters should not be used

namespace Steeltoe.NetCoreToolService.Controllers;

/// <summary>
/// The controller for "dotnet new".
/// </summary>
[ApiController]
[Route("api/new")]
public sealed partial class NewController : ControllerBase
{
    private const string DefaultOutput = "Sample";
    private const string DefaultPackaging = "zip";

    private static readonly string[] LineBreaks =
    [
        "\r",
        "\n",
        "\r\n"
    ];

    private static readonly SearchValues<string> LineBreakValues = SearchValues.Create(LineBreaks, StringComparison.Ordinal);

    private readonly ICommandExecutor _commandExecutor;
    private readonly ILogger<NewController> _logger;

    private readonly Dictionary<string, IPackager> _packagers = new()
    {
        { "zip", new ZipPackager() }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="NewController" /> class.
    /// </summary>
    /// <param name="commandExecutor">
    /// Injected command.
    /// </param>
    /// <param name="logger">
    /// Injected logger.
    /// </param>
    public NewController(ICommandExecutor commandExecutor, ILogger<NewController> logger)
    {
        ArgumentNullException.ThrowIfNull(commandExecutor);
        ArgumentNullException.ThrowIfNull(logger);

        _commandExecutor = commandExecutor;
        _logger = logger;
    }

    /// <summary>
    /// Gets the available Net Core Tool templates.
    /// </summary>
    /// <returns>
    /// Templates.
    /// </returns>
    [HttpGet]
    public async Task<ActionResult> GetTemplates()
    {
        return Ok(await GetTemplateDictionaryAsync());
    }

    /// <summary>
    /// Installs the Net Core Tool templates for the specified NuGet ID.
    /// </summary>
    /// <param name="nuGetId">
    /// Template NuGet ID.
    /// </param>
    /// <returns>
    /// Information about the installed templates.
    /// </returns>
    [HttpPut("nuget/{nuGetId}")]
    public async Task<ActionResult> InstallTemplates(string nuGetId)
    {
        ArgumentNullException.ThrowIfNull(nuGetId);

        await _commandExecutor.ExecuteAsync($"{NetCoreTool.Command} new uninstall {nuGetId}", null, -1);
        TemplateDictionary oldTemplates = await GetTemplateDictionaryAsync();
        CommandResult installCommand = await _commandExecutor.ExecuteAsync($"{NetCoreTool.Command} new install {nuGetId}", null, -1);
        const string notFoundError = "error NU1101: ";

        if (installCommand.Output.Contains(notFoundError, StringComparison.Ordinal))
        {
            ReadOnlySpan<char> outputSpan = installCommand.Output.AsSpan();
            ReadOnlySpan<char> messageSpan = GetStringOnLine(StripTextBefore(outputSpan, notFoundError)).Trim();
            return BadRequest(messageSpan.ToString());
        }

        TemplateDictionary newTemplates = await GetTemplateDictionaryAsync();

        foreach (string oldTemplate in oldTemplates.Keys)
        {
            newTemplates.Remove(oldTemplate);
        }

        return CreatedAtAction(nameof(InstallTemplates), newTemplates);
    }

    /// <summary>
    /// Uninstalls the Net Core Tool templates for the specified NuGet ID.
    /// </summary>
    /// <param name="nuGetId">
    /// Template NuGet ID.
    /// </param>
    /// <returns>
    /// Information about the installed templates.
    /// </returns>
    [HttpDelete("nuget/{nuGetId}")]
    public async Task<ActionResult> UninstallTemplates(string nuGetId)
    {
        ArgumentNullException.ThrowIfNull(nuGetId);

        TemplateDictionary oldTemplates = await GetTemplateDictionaryAsync();
        CommandResult uninstallCommand = await _commandExecutor.ExecuteAsync($"{NetCoreTool.Command} new uninstall {nuGetId}", null, -1);

        if (uninstallCommand.Output.Contains("Could not find something to uninstall", StringComparison.Ordinal))
        {
            return NotFound($"No templates with NuGet ID '{nuGetId}' installed.");
        }

        TemplateDictionary newTemplates = await GetTemplateDictionaryAsync();

        foreach (string newTemplate in newTemplates.Keys)
        {
            oldTemplates.Remove(newTemplate);
        }

        return Ok(oldTemplates);
    }

    /// <summary>
    /// Returns "help" for the specified Net Core Tool template.
    /// </summary>
    /// <param name="template">
    /// Template name.
    /// </param>
    /// <returns>
    /// Template help.
    /// </returns>
    [HttpGet("{template}/help")]
    public async Task<ActionResult> GetTemplateHelp(string template)
    {
        ArgumentNullException.ThrowIfNull(template);

        CommandResult helpCommand = await _commandExecutor.ExecuteAsync($"{NetCoreTool.Command} new {template} --help", null, -1);

        if (helpCommand.ExitCode != 0)
        {
            ReadOnlySpan<char> errorSpan = helpCommand.Error.AsSpan();
            ReadOnlySpan<char> messageSpan = GetStringOnLine(StripTextBefore(errorSpan, "No templates found", true)).Trim();
            return NotFound(messageSpan.ToString());
        }

        return Ok(helpCommand.Output.Trim());
    }

    /// <summary>
    /// Gets a generated project for the specified Net Core Tool template.
    /// </summary>
    /// <param name="template">
    /// Template name.
    /// </param>
    /// <param name="options">
    /// Template options.
    /// </param>
    /// <param name="packaging">
    /// Project packaging, e.g. zip.
    /// </param>
    /// <returns>
    /// Project archive.
    /// </returns>
    [HttpGet]
    [Route("{template}")]
    public async Task<ActionResult> GetTemplateProject(string template, string? options = null, string packaging = DefaultPackaging)
    {
        ArgumentNullException.ThrowIfNull(template);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        LogNewTemplate(template, options, packaging);

        try
        {
            string output = DefaultOutput;
            var optionList = new List<string>();

            if (options is not null)
            {
                foreach (string option in options.Split(','))
                {
                    if (option.Contains('='))
                    {
                        string[] nvp = option.Split('=', 2);

                        if (nvp[0] == "output")
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

            if (!_packagers.TryGetValue(packaging, out IPackager? packager))
            {
                return BadRequest($"Unknown or unsupported packaging '{packaging}'.");
            }

            using var projectDir = new TempDirectory("NetCoreToolService-");
            var commandLine = new StringBuilder();
            commandLine.Append(NetCoreTool.Command).Append(" new ").Append(template);
            commandLine.Append(" --output=").Append(output);

            foreach (string option in optionList)
            {
                commandLine.Append(' ').Append(option);
            }

            return await ExecuteCommandAsync(template, commandLine.ToString(), projectDir, packager, output);
        }
        finally
        {
            stopwatch.Stop();
            LogProjectGenerated(stopwatch.Elapsed);
        }
    }

    private async Task<ActionResult> ExecuteCommandAsync(string template, string commandLine, TempDirectory projectDir, IPackager packager, string output)
    {
        CommandResult newCommand = await _commandExecutor.ExecuteAsync(commandLine, projectDir.FullPath, -1);

        const string unknownTemplateError = "No templates found";

        if (newCommand.Error.Contains(unknownTemplateError, StringComparison.Ordinal))
        {
            return NotFound($"Template '{template}' not found.");
        }

        const string invalidOptionError = "Invalid option(s)";

        if (newCommand.Error.Contains(invalidOptionError, StringComparison.Ordinal))
        {
            ReadOnlySpan<char> errorSpan = newCommand.Error.AsSpan();
            ReadOnlySpan<char> switchName = GetStringOnLine(StripTextBefore(StripTextBefore(errorSpan, invalidOptionError), "--"));
            return NotFound($"Switch '{switchName}' not found.");
        }

        const string invalidSwitchError = "Invalid input switch:";

        if (newCommand.Error.Contains(invalidSwitchError, StringComparison.Ordinal))
        {
            ReadOnlySpan<char> errorSpan = newCommand.Error.AsSpan();
            ReadOnlySpan<char> switchName = GetStringOnLine(StripTextBefore(StripTextBefore(errorSpan, invalidSwitchError), "--"));
            return NotFound($"Switch '{switchName}' not found.");
        }

        const string invalidParameterError = "Error: Invalid parameter(s):";

        if (newCommand.Error.Contains(invalidParameterError, StringComparison.Ordinal))
        {
            ReadOnlySpan<char> errorSpan = newCommand.Error.AsSpan();
            ReadOnlySpan<char> parameters = GetStringOnLine(StripTextBefore(StripTextBefore(errorSpan, invalidParameterError), "--"));
            (string key, string value) = SplitNameValuePair(parameters, ' ');
            return NotFound($"Option '{key}' parameter '{value}' not found.");
        }

        if (newCommand.ExitCode != 0)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, newCommand.Error.Trim());
        }

        if (!newCommand.Output.Contains(" was created successfully.", StringComparison.Ordinal))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, newCommand.Output.Trim());
        }

        byte[] package = packager.ToBytes(projectDir.FullPath);
        return File(package, packager.MimeType, $"{output}{packager.FileExtension}");
    }

    private async Task<TemplateDictionary> GetTemplateDictionaryAsync()
    {
        CommandResult listCommand = await _commandExecutor.ExecuteAsync($"{NetCoreTool.Command} new list", null, -1);
        List<string> lines = listCommand.Output.Split(LineBreaks, StringSplitOptions.None).ToList().FindAll(line => !string.IsNullOrWhiteSpace(line));

        int headingIdx = lines.FindIndex(line => line.StartsWith('-'));
        string[] headings = lines[headingIdx].Split("  ");
        const int nameColStart = 0;
        int nameColEnd = nameColStart + headings[0].Length;
        int shortNameColStart = nameColEnd + 2;
        int shortNameColEnd = shortNameColStart + headings[1].Length;
        int languageColStart = shortNameColEnd + 2;
        int languageColEnd = languageColStart + headings[2].Length;
        int tagsColStart = languageColEnd + 2;
        int tagsColEnd = tagsColStart + headings[3].Length;
        lines = lines.GetRange(headingIdx + 1, lines.Count - headingIdx - 1);

        var dictionary = new TemplateDictionary();

        foreach (string line in lines)
        {
            string template = line[shortNameColStart..shortNameColEnd].Trim();

            var templateInfo = new TemplateInfo
            {
                Name = line[..nameColEnd].Trim(),
                Languages = line[languageColStart..languageColEnd].Trim(),
                Tags = line[tagsColStart..Math.Min(tagsColEnd, line.Length)].Trim()
            };

            dictionary.Add(template, templateInfo);
        }

        return dictionary;
    }

    private static ReadOnlySpan<char> StripTextBefore(ReadOnlySpan<char> source, string textToFind, bool keepTextToFind = false)
    {
        int startIndex = source.IndexOf(textToFind, StringComparison.Ordinal);

        if (startIndex == -1)
        {
            return source;
        }

        return keepTextToFind ? source[startIndex..] : source[(startIndex + textToFind.Length)..];
    }

    private static ReadOnlySpan<char> GetStringOnLine(ReadOnlySpan<char> source)
    {
        int lineBreakIndex = source.IndexOfAny(LineBreakValues);
        return lineBreakIndex == -1 ? source : source[..lineBreakIndex];
    }

    private static (string Key, string Value) SplitNameValuePair(ReadOnlySpan<char> source, char separator)
    {
        Span<Range> destination = stackalloc Range[2];
        int count = source.Split(destination, separator);

        switch (count)
        {
            case 2:
            {
                string key = source[destination[0]].ToString();
                string value = source[destination[1]].ToString();
                return (key, value);
            }
            case 1:
            {
                string key = source[destination[0]].ToString();
                return (key, string.Empty);
            }
            default:
            {
                return (string.Empty, string.Empty);
            }
        }
    }

    [LoggerMessage(LogLevel.Information, "New: template={Template}, options={Options}, packaging={Packaging}")]
    partial void LogNewTemplate(string template, string? options, string packaging);

    [LoggerMessage(LogLevel.Debug, "Generated project in {Elapsed:c}")]
    partial void LogProjectGenerated(TimeSpan elapsed);
}
