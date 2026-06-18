// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Steeltoe.NetCoreToolService.Models;
using Steeltoe.NetCoreToolService.Packagers;
using Steeltoe.NetCoreToolService.SteeltoeUtils.Diagnostics;
using Steeltoe.NetCoreToolService.SteeltoeUtils.IO;

namespace Steeltoe.NetCoreToolService.Controllers;

/// <summary>
/// The controller for "dotnet new".
/// </summary>
[ApiController]
[Route("api/new")]
public sealed partial class NewController : ControllerBase
{
    private const string DefaultOutputName = "Sample";
    private const string DefaultPackagingFormat = "zip";
    private const string SensitiveEndpointUnavailableMessage = "Set 'EnableSensitiveEndpoints' to 'true' in configuration to enable this endpoint.";
    private const string MultiLineTrimChars = " \r\n";

    /// <summary>
    /// The name of the 'dotnet' executable.
    /// </summary>
    public const string ExecutableName = "dotnet";

    private static readonly AsyncLock WriteLock = new();
    private static readonly SearchValues<char> LineBreakValues = SearchValues.Create('\r', '\n');

    private readonly ICommandExecutor _commandExecutor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NewController> _logger;

    private readonly Dictionary<string, IPackager> _packagers = new()
    {
        ["zip"] = new ZipPackager()
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="NewController" /> class.
    /// </summary>
    /// <param name="commandExecutor">
    /// Executes 'dotnet' commands.
    /// </param>
    /// <param name="configuration">
    /// The app configuration.
    /// </param>
    /// <param name="logger">
    /// The logger.
    /// </param>
    public NewController(ICommandExecutor commandExecutor, IConfiguration configuration, ILogger<NewController> logger)
    {
        ArgumentNullException.ThrowIfNull(commandExecutor);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _commandExecutor = commandExecutor;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets the available project templates.
    /// </summary>
    /// <returns>
    /// The project templates.
    /// </returns>
    [HttpGet]
    public async Task<ActionResult> GetTemplates()
    {
        TemplateDictionary dictionary = await GetTemplateDictionaryAsync();
        return Ok(dictionary);
    }

    /// <summary>
    /// Installs the templates provided by the specified NuGet package.
    /// </summary>
    /// <param name="nuGetId">
    /// The NuGet package that contains project templates.
    /// </param>
    /// <returns>
    /// Information about the templates that were installed.
    /// </returns>
    [HttpPut("nuget/{nuGetId}")]
    public async Task<ActionResult> InstallTemplates(string nuGetId)
    {
        ArgumentNullException.ThrowIfNull(nuGetId);

        if (!AreSensitiveEndpointsEnabled())
        {
            return StatusCode((int)HttpStatusCode.ServiceUnavailable, SensitiveEndpointUnavailableMessage);
        }

        using IDisposable lockScope = await WriteLock.AcquireAsync();
        await _commandExecutor.ExecuteAsync($"{ExecutableName} new uninstall {nuGetId}", null, -1);
        TemplateDictionary oldTemplates = await GetTemplateDictionaryAsync();

        CommandResult installCommand = await _commandExecutor.ExecuteAsync($"{ExecutableName} new install {nuGetId}", null, -1);

        if (installCommand.ExitCode == 103)
        {
            return BadRequest($"The package '{nuGetId}' does not exist.");
        }

        if (installCommand.ExitCode != 0)
        {
            ReadOnlySpan<char> errorSpan = installCommand.Error.AsSpan();
            return StatusCode(StatusCodes.Status500InternalServerError, errorSpan.Trim(MultiLineTrimChars).ToString());
        }

        TemplateDictionary newTemplates = await GetTemplateDictionaryAsync();

        foreach (string oldTemplate in oldTemplates.Keys)
        {
            newTemplates.Remove(oldTemplate);
        }

        return CreatedAtAction(nameof(InstallTemplates), newTemplates);
    }

    private bool AreSensitiveEndpointsEnabled()
    {
        return _configuration.GetValue("EnableSensitiveEndpoints", false);
    }

    /// <summary>
    /// Uninstalls the templates provided by the specified NuGet package.
    /// </summary>
    /// <param name="nuGetId">
    /// The NuGet package that contains project templates.
    /// </param>
    /// <returns>
    /// Information about the templates that were uninstalled.
    /// </returns>
    [HttpDelete("nuget/{nuGetId}")]
    public async Task<ActionResult> UninstallTemplates(string nuGetId)
    {
        ArgumentNullException.ThrowIfNull(nuGetId);

        if (!AreSensitiveEndpointsEnabled())
        {
            return StatusCode((int)HttpStatusCode.ServiceUnavailable, SensitiveEndpointUnavailableMessage);
        }

        using IDisposable lockScope = await WriteLock.AcquireAsync();
        TemplateDictionary oldTemplates = await GetTemplateDictionaryAsync();
        CommandResult uninstallCommand = await _commandExecutor.ExecuteAsync($"{ExecutableName} new uninstall {nuGetId}", null, -1);

        if (uninstallCommand.ExitCode == 103)
        {
            return NotFound($"No templates from package '{nuGetId}' are installed.");
        }

        if (uninstallCommand.ExitCode != 0)
        {
            ReadOnlySpan<char> errorSpan = uninstallCommand.Error.AsSpan();
            return StatusCode(StatusCodes.Status500InternalServerError, errorSpan.Trim(MultiLineTrimChars).ToString());
        }

        TemplateDictionary newTemplates = await GetTemplateDictionaryAsync();

        foreach (string newTemplateName in newTemplates.Keys)
        {
            oldTemplates.Remove(newTemplateName);
        }

        return Ok(oldTemplates);
    }

    /// <summary>
    /// Returns "help" for the specified project template.
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

        CommandResult helpCommand = await _commandExecutor.ExecuteAsync($"{ExecutableName} new {template} --help", null, -1);
        ReadOnlySpan<char> outputSpan = helpCommand.Output.AsSpan();

        if (helpCommand.ExitCode != 0)
        {
            ReadOnlySpan<char> errorSpan = helpCommand.Error.AsSpan();
            return StatusCode(StatusCodes.Status500InternalServerError, errorSpan.Trim(MultiLineTrimChars).ToString());
        }

        if (outputSpan.StartsWith("No templates or subcommands found matching:"))
        {
            return NotFound($"Template '{template}' not found.");
        }

        return Ok(outputSpan.Trim().ToString());
    }

    /// <summary>
    /// Generates a project from the specified project template.
    /// </summary>
    /// <param name="template">
    /// Short name of the template.
    /// </param>
    /// <param name="options">
    /// Comma-separated list of options.
    /// </param>
    /// <param name="packaging">
    /// Project packaging, e.g. zip.
    /// </param>
    /// <returns>
    /// The generated project archive.
    /// </returns>
    [HttpGet]
    [Route("{template}")]
#pragma warning disable S2360 // Optional parameters should not be used
    public async Task<ActionResult> GetTemplateProject(string template, string? options = null, string packaging = DefaultPackagingFormat)
#pragma warning restore S2360 // Optional parameters should not be used
    {
        ArgumentNullException.ThrowIfNull(template);

        if (!_packagers.TryGetValue(packaging, out IPackager? packager))
        {
            return BadRequest($"Unknown or unsupported packaging format '{packaging}'.");
        }

        LogNewTemplate(template, options, packaging);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            List<string> optionList = ParseOptions(options, out string outputName);
            using var projectDirectory = new TempDirectory("NetCoreToolService-");
            var commandLineBuilder = new StringBuilder($"{ExecutableName} new {template}");

            foreach (string option in optionList)
            {
                commandLineBuilder.Append(' ');
                commandLineBuilder.Append(option);
            }

            return await ExecuteCreateProjectCommandAsync(template, commandLineBuilder.ToString(), projectDirectory, packager, outputName);
        }
        finally
        {
            stopwatch.Stop();
            LogProjectGenerated(stopwatch.Elapsed);
        }
    }

    private static List<string> ParseOptions(string? options, out string outputName)
    {
        outputName = DefaultOutputName;
        var optionList = new List<string>();

        if (options is not null)
        {
            ReadOnlySpan<char> optionsSpan = options.AsSpan();

            foreach (Range optionRange in optionsSpan.Split(','))
            {
                ReadOnlySpan<char> optionSpan = optionsSpan[optionRange].Trim();

                if (optionSpan.Length > 0)
                {
                    int equalsIndex = optionSpan.IndexOf('=');

                    if (equalsIndex != -1)
                    {
                        ReadOnlySpan<char> keySpan = optionSpan[..equalsIndex].Trim();
                        ReadOnlySpan<char> valueSpan = optionSpan[(equalsIndex + 1)..].Trim();

                        if (keySpan.Length > 0 && valueSpan.Length > 0)
                        {
                            ReadOnlySpan<char> escapedValueSpan = valueSpan.Contains(' ') ? $"\"{valueSpan}\"" : valueSpan;

                            if (keySpan.Equals("output", StringComparison.Ordinal))
                            {
                                outputName = escapedValueSpan.ToString();
                            }
                            else
                            {
                                optionList.Add($"--{keySpan}={escapedValueSpan}");
                            }
                        }
                    }
                    else
                    {
                        optionList.Add($"--{optionSpan}");
                    }
                }
            }
        }

        optionList.Insert(0, $"--output={outputName}");
        return optionList;
    }

    private async Task<ActionResult> ExecuteCreateProjectCommandAsync(string template, string commandLine, TempDirectory projectDirectory, IPackager packager,
        string outputName)
    {
        CommandResult newCommand = await _commandExecutor.ExecuteAsync(commandLine, projectDirectory.FullPath, -1);

        // Exit codes are documented at: https://aka.ms/templating-exit-codes
        ReadOnlySpan<char> outputSpan = newCommand.Output.AsSpan();
        ReadOnlySpan<char> errorSpan = newCommand.Error.AsSpan();

        if (newCommand.ExitCode == 103)
        {
            if (errorSpan.StartsWith("No templates or subcommands found matching:"))
            {
                return NotFound($"Template '{template}' not found.");
            }

            List<string> optionLines = [];

            if (errorSpan.StartsWith("No templates found matching:"))
            {
                foreach (Range lineRange in errorSpan.SplitAny(LineBreakValues))
                {
                    ReadOnlySpan<char> lineSpan = errorSpan[lineRange];

                    if (lineSpan.StartsWith("Allowed values for "))
                    {
                        optionLines.Add(lineSpan.ToString());
                    }
                }

                if (optionLines.Count > 0)
                {
                    return NotFound(string.Join(' ', optionLines));
                }
            }
        }

        if (newCommand.ExitCode == 127 && errorSpan.StartsWith("Error: Invalid option(s):"))
        {
            List<string> optionLines = [];

            foreach (Range lineRange in errorSpan.SplitAny(LineBreakValues))
            {
                ReadOnlySpan<char> lineSpan = errorSpan[lineRange];

                if (lineSpan.EndsWith(" is not a valid option"))
                {
                    optionLines.Add(lineSpan.TrimStart().ToString() + '.');
                }
            }

            if (optionLines.Count > 0)
            {
                return NotFound(string.Join(' ', optionLines));
            }
        }

        if (newCommand.ExitCode != 0)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, errorSpan.Trim(MultiLineTrimChars).ToString());
        }

        if (!outputSpan.Contains(" was created successfully.", StringComparison.Ordinal))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, outputSpan.Trim(MultiLineTrimChars).ToString());
        }

        byte[] package = packager.ToBytes(projectDirectory.FullPath);
        return File(package, packager.MimeType, $"{outputName}{packager.FileExtension}");
    }

    private async Task<TemplateDictionary> GetTemplateDictionaryAsync()
    {
        CommandResult listCommand = await _commandExecutor.ExecuteAsync($"{ExecutableName} new list", null, -1);

        if (listCommand.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to list templates: {listCommand.Error}");
        }

        ReadOnlySpan<char> outputSpan = listCommand.Output.AsSpan();
        ListTable? listTable = null;
        var dictionary = new TemplateDictionary();

        foreach (Range lineRange in outputSpan.SplitAny(LineBreakValues))
        {
            ReadOnlySpan<char> lineSpan = outputSpan[lineRange].Trim();

            if (lineSpan.Length > 0)
            {
                if (lineSpan.StartsWith('-'))
                {
                    List<Range> columnRanges = [];

                    foreach (Range columnRange in lineSpan.Split("  "))
                    {
                        columnRanges.Add(columnRange);
                    }

                    if (columnRanges.Count < 4)
                    {
                        throw new InvalidOperationException("Failed to parse template table.");
                    }

                    listTable = new ListTable(columnRanges[0], columnRanges[1], columnRanges[2], columnRanges[3]);
                }
                else if (listTable != null)
                {
                    ReadOnlySpan<char> shortNameSpan = lineSpan[listTable.Value.ShortNameRange].TrimEnd();
                    ReadOnlySpan<char> templateNameSpan = lineSpan[listTable.Value.TemplateNameRange].TrimEnd();
                    ReadOnlySpan<char> languageSpan = lineSpan[listTable.Value.LanguageRange].TrimEnd();
                    ReadOnlySpan<char> tagsSpan = lineSpan[listTable.Value.GetTagsRangeForLine(lineSpan.Length)];

                    var templateInfo = new TemplateInfo(templateNameSpan.ToString(), languageSpan.ToString(), tagsSpan.ToString());
                    dictionary.Add(shortNameSpan.ToString(), templateInfo);
                }
            }
        }

        return dictionary;
    }

    [LoggerMessage(LogLevel.Information, "New: template={Template}, options={Options}, packaging={Packaging}")]
    partial void LogNewTemplate(string template, string? options, string packaging);

    [LoggerMessage(LogLevel.Debug, "Generated project in {Elapsed:c}")]
    partial void LogProjectGenerated(TimeSpan elapsed);

    private readonly record struct ListTable(Range TemplateNameRange, Range ShortNameRange, Range LanguageRange, Range TagsRange)
    {
        public Range GetTagsRangeForLine(int lineLength)
        {
            return TagsRange.End.Value >= lineLength ? new Range(TagsRange.Start, lineLength) : TagsRange;
        }
    }
}
