// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.NetCoreToolService.Controllers;
using Steeltoe.NetCoreToolService.Models;
using Steeltoe.NetCoreToolService.SteeltoeUtils.Diagnostics;
using Xunit;

namespace Steeltoe.NetCoreToolService.Test.Controllers;

public sealed class NewControllerTest
{
    private static readonly IConfiguration EmptyConfiguration = new ConfigurationBuilder().Build();

    private static readonly IConfiguration SensitiveEndpointsEnabledConfiguration = new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?>
        {
            ["EnableSensitiveEndpoints"] = "true"
        }).Build();

    [Fact]
    public async Task GetTemplates_Should_Return_All_Templates()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NewController.ExecutableName} new list", null, -1)).ReturnsAsync(new CommandResult
        {
            Output = """
                These templates matched your input:

                Template Name   Short Name     Language    Tags
                --------------  -------------  ----------  --------------
                API Controller  apicontroller  [C#]        Web/ASP.NET
                Class Library   classlib       [C#],F#,VB  Common/Library
                """
        });

        var controller = new NewController(executor.Object, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplates();

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        TemplateDictionary templates = okResult.Value.Should().BeOfType<TemplateDictionary>().Subject;

        templates.Should().BeEquivalentTo(new TemplateDictionary
        {
            ["apicontroller"] = new TemplateInfo("API Controller", "[C#]", "Web/ASP.NET"),
            ["classlib"] = new TemplateInfo("Class Library", "[C#],F#,VB", "Common/Library")
        });
    }

    [Fact]
    public async Task InstallTemplates_Is_Disabled_By_Default()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.InstallTemplates("My.Templates");

        // Assert
        ObjectResult errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
        errorResult.Value.Should().Be("Set 'EnableSensitiveEndpoints' to 'true' in configuration to enable this endpoint.");
    }

    [Fact]
    public async Task InstallTemplates_Should_Return_Installed_Templates()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.SetupSequence(c => c.ExecuteAsync($"{NewController.ExecutableName} new list", null, -1)).ReturnsAsync(new CommandResult
        {
            Output = """
                These templates matched your input:

                Template Name      Short Name  Language    Tags
                -----------------  ----------  ----------  ----------
                My Template        myt         lang        tags
                My Other Template  my-ot       other-lang  other-tags
                """
        }).ReturnsAsync(new CommandResult
        {
            Output = """
                These templates matched your input:

                Template Name       Short Name  Language    Tags
                ------------------  ----------  ----------  -----------
                A New Template      ant         smalltalk   new-stuff
                My Template         myt         lang        tags
                My Other Template   my-ot       other-lang  other-tags
                Other New Template  ont         big-talk    other-stuff
                """
        });

        executor.Setup(c => c.ExecuteAsync($"{NewController.ExecutableName} new install My.Templates", null, -1)).ReturnsAsync(default(CommandResult));

        var controller = new NewController(executor.Object, SensitiveEndpointsEnabledConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.InstallTemplates("My.Templates");

        // Assert
        CreatedAtActionResult createdAtResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        TemplateDictionary templates = createdAtResult.Value.Should().BeOfType<TemplateDictionary>().Subject;

        templates.Should().BeEquivalentTo(new TemplateDictionary
        {
            ["ant"] = new TemplateInfo("A New Template", "smalltalk", "new-stuff"),
            ["ont"] = new TemplateInfo("Other New Template", "big-talk", "other-stuff")
        });
    }

    [Fact]
    public async Task InstallTemplates_UnknownNuGetId_Should_Return_BadRequest()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, SensitiveEndpointsEnabledConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.InstallTemplates("No.Such.Template");

        // Assert
        BadRequestObjectResult errorResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        errorResult.Value.Should().Be("The package 'No.Such.Template' does not exist.");
    }

    [Fact]
    public async Task UninstallTemplates_Is_Disabled_By_Default()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.UninstallTemplates("My.Templates");

        // Assert
        ObjectResult errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
        errorResult.Value.Should().Be("Set 'EnableSensitiveEndpoints' to 'true' in configuration to enable this endpoint.");
    }

    [Fact]
    public async Task UninstallTemplates_Should_Return_Uninstalled_Templates()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.SetupSequence(c => c.ExecuteAsync($"{NewController.ExecutableName} new list", null, -1)).ReturnsAsync(new CommandResult
        {
            Output = """
                These templates matched your input:

                Template Name       Short Name  Language    Tags
                ------------------  ----------  ----------  -----------
                A New Template      ant         smalltalk   new-stuff
                My Template         myt         lang        tags
                My Other Template   my-ot       other-lang  other-tags
                Other New Template  ont         big-talk    other-stuff
                """
        }).ReturnsAsync(new CommandResult
        {
            Output = """
                These templates matched your input:

                Template Name      Short Name  Language    Tags
                -----------------  ----------  ----------  ----------
                My Template        myt         lang        tags
                My Other Template  my-ot       other-lang  other-tags
                """
        });

        executor.Setup(c => c.ExecuteAsync($"{NewController.ExecutableName} new uninstall My.Templates", null, -1)).ReturnsAsync(default(CommandResult));

        var controller = new NewController(executor.Object, SensitiveEndpointsEnabledConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.UninstallTemplates("My.Templates");

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        TemplateDictionary templates = okResult.Value.Should().BeOfType<TemplateDictionary>().Subject;

        templates.Should().BeEquivalentTo(new TemplateDictionary
        {
            ["ant"] = new TemplateInfo("A New Template", "smalltalk", "new-stuff"),
            ["ont"] = new TemplateInfo("Other New Template", "big-talk", "other-stuff")
        });
    }

    [Fact]
    public async Task UninstallTemplates_UnknownNuGetId_Should_Return_NotFound()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, SensitiveEndpointsEnabledConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.UninstallTemplates("My.Templates");

        // Assert
        NotFoundObjectResult notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("No templates from package 'My.Templates' are installed.");
    }

    [Fact]
    public async Task GetTemplateHelp_Should_Return_Help()
    {
        // Arrange
        const string helpText = """
            Class Library (C#)
            Author: Microsoft
            Description: A project for creating a class library that targets .NET or .NET Standard

            Usage:
              dotnet new classlib [options] [template options]

            Options:
              -n, --name <name>       The name for the output being created. If no name is specified, the name of the output directory is used.
              -o, --output <output>   Location to place the generated output.
              --dry-run               Displays a summary of what would happen if the given command line were run if it would result in a template creation. [default: False]
              --force                 Forces content to be generated even if it would change existing files. [default: False]
              --no-update-check       Disables checking for the template package updates when instantiating a template. [default: False]
              --project <project>     The project that should be used for context evaluation.
              -lang, --language <C#>  Specifies the template language to instantiate.
              --type <project>        Specifies the template type to instantiate.

            Template options:
              -f, --framework <net10.0|net6.0|net8.0|net9.0|netstandard2.0|netstandard2.1>  The target framework for the project.
                                                                                            Type: choice
                                                                                              net10.0         Target net10.0
                                                                                              netstandard2.1  Target netstandard2.1
                                                                                              netstandard2.0  Target netstandard2.0
                                                                                              net9.0          Target net9.0
                                                                                              net8.0          Target net8.0
                                                                                              net6.0          Target net6.0
                                                                                            Default: net10.0
              --langVersion <langVersion>                                                   Sets the LangVersion property in the created project file
                                                                                            Type: text
              --no-restore                                                                  If specified, skips the automatic restore of the project on create.
                                                                                            Type: bool
                                                                                            Default: false

            To see help for other template languages (F#, VB), use --language option:
               dotnet new classlib -h --language F#
            """;

        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NewController.ExecutableName} new classlib --help", null, -1)).ReturnsAsync(new CommandResult
        {
            Output = helpText
        });

        var controller = new NewController(executor.Object, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateHelp("classlib");

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        string message = okResult.Value.Should().BeOfType<string>().Subject;
        message.Should().Be(helpText);
    }

    [Fact]
    public async Task GetTemplateHelp_UnknownTemplate_Should_Return_NotFound()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateHelp("no-such-template");

        // Assert
        NotFoundObjectResult notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Template 'no-such-template' not found.");
    }

    [Fact]
    public async Task GetTemplateProject_Should_Return_Project_Package()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("classlib");

        // Assert
        FileContentResult fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/zip");
        fileResult.FileDownloadName.Should().Be("Sample.zip");
        fileResult.FileContents.Should().HaveCountGreaterThan(0);

        using var stream = new MemoryStream(fileResult.FileContents);
        await using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.Entries.Should().NotBeEmpty();
        archive.Entries.Should().ContainSingle(entry => entry.Name == "Sample.csproj");
    }

    [Fact]
    public async Task GetTemplateProject_Can_Specify_Output()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("classlib", "output=Joe");

        // Assert
        FileContentResult fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/zip");
        fileResult.FileDownloadName.Should().Be("Joe.zip");
        fileResult.FileContents.Should().HaveCountGreaterThan(0);

        using var stream = new MemoryStream(fileResult.FileContents);
        await using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.Entries.Should().NotBeEmpty();
        archive.Entries.Should().ContainSingle(entry => entry.Name == "Joe.csproj");
    }

    [Fact]
    public async Task GetTemplateProject_Can_Specify_Output_With_Whitespace()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("classlib", "output= Joe's Demo Project ");

        // Assert
        FileContentResult fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/zip");
        fileResult.FileDownloadName.Should().Be("Joe's Demo Project.zip");
        fileResult.FileContents.Should().HaveCountGreaterThan(0);

        using var stream = new MemoryStream(fileResult.FileContents);
        await using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.Entries.Should().NotBeEmpty();
        archive.Entries.Should().ContainSingle(entry => entry.Name == "Joe's Demo Project.csproj");
    }

    [Fact]
    public async Task GetTemplateProject_Strips_Unix_Path_From_Output()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("classlib", "output=/tmp/Joe");

        // Assert
        FileContentResult fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/zip");
        fileResult.FileDownloadName.Should().Be("Joe.zip");
        fileResult.FileContents.Should().HaveCountGreaterThan(0);

        using var stream = new MemoryStream(fileResult.FileContents);
        await using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.Entries.Should().NotBeEmpty();
        archive.Entries.Should().ContainSingle(entry => entry.Name == "Joe.csproj");
    }

    [Fact]
    public async Task GetTemplateProject_Strips_Windows_Path_From_Output()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("classlib", @"output=c:\temp\Joe");

        // Assert
        FileContentResult fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/zip");
        fileResult.FileDownloadName.Should().Be("Joe.zip");
        fileResult.FileContents.Should().HaveCountGreaterThan(0);

        using var stream = new MemoryStream(fileResult.FileContents);
        await using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.Entries.Should().NotBeEmpty();
        archive.Entries.Should().ContainSingle(entry => entry.Name == "Joe.csproj");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownTemplate_Should_Return_NotFound()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("no-such-template");

        // Assert
        NotFoundObjectResult notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Template 'no-such-template' not found.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownSwitches_Should_Return_NotFound()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("classlib", "unknown-switch, other-switch");

        // Assert
        NotFoundObjectResult notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("'--unknown-switch' is not a valid option. '--other-switch' is not a valid option.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownParameters_Should_Return_NotFound()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("classlib", "language=unknown, type=other");

        // Assert
        NotFoundObjectResult notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Allowed values for '--language' option are: 'C#', 'F#', 'VB'. Allowed values for '--type' option are: 'project'.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownPackagingFormat_Should_Return_BadRequest()
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template", packaging: "acme-packaging");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        badRequest.Value.Should().Be("Unknown or unsupported packaging format 'acme-packaging'.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownError_Should_Return_InternalServerError()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 1,
            Error = "Something bad happened."
        });

        var controller = new NewController(executor.Object, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template");

        // Assert
        ObjectResult errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        errorResult.Value.Should().Be("Something bad happened.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownOutput_Should_Return_InternalServerError()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), -1)).ReturnsAsync(new CommandResult
        {
            Output = "Unexpected output.",
            Error = string.Empty
        });

        var controller = new NewController(executor.Object, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template");

        // Assert
        ObjectResult errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        errorResult.Value.Should().Be("Unexpected output.");
    }

    [Theory]
    [InlineData("install evil-pkg")]
    [InlineData("--allow-scripts")]
    [InlineData("new install")]
    [InlineData("../evil")]
    [InlineData("evil;id")]
    public async Task GetTemplateProject_InjectedTemplateName_Should_Return_BadRequest(string template)
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject(template);

        // Assert
        BadRequestObjectResult badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be($"Invalid template name '{template}'.");
    }

    [Theory]
    [InlineData("install evil-pkg")]
    [InlineData("--allow-scripts")]
    [InlineData("../evil")]
    public async Task GetTemplateHelp_InjectedTemplateName_Should_Return_BadRequest(string template)
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateHelp(template);

        // Assert
        BadRequestObjectResult badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be($"Invalid template name '{template}'.");
    }

    [Theory]
    [InlineData("allow-scripts=yes")]
    [InlineData("allow-scripts")]
    [InlineData("--allow-scripts=yes")]
    [InlineData("force=true")]
    [InlineData("name=foo,allow-scripts=yes")]
    public async Task GetTemplateProject_BlockedOrInvalidOption_Should_Return_BadRequest(string options)
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, EmptyConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("classlib", options);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("evil pkg")]
    [InlineData("evil;pkg")]
    [InlineData("../Evil.Templates")]
    [InlineData("evil|pkg")]
    public async Task InstallTemplates_InvalidNuGetId_Should_Return_BadRequest(string nuGetId)
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, SensitiveEndpointsEnabledConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.InstallTemplates(nuGetId);

        // Assert
        BadRequestObjectResult badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be($"Invalid NuGet package ID '{nuGetId}'.");
    }

    [Theory]
    [InlineData("evil pkg")]
    [InlineData("evil;pkg")]
    [InlineData("../Evil.Templates")]
    public async Task UninstallTemplates_InvalidNuGetId_Should_Return_BadRequest(string nuGetId)
    {
        // Arrange
        var executor = new CommandExecutor(NullLogger<CommandExecutor>.Instance);
        var controller = new NewController(executor, SensitiveEndpointsEnabledConfiguration, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.UninstallTemplates(nuGetId);

        // Assert
        BadRequestObjectResult badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be($"Invalid NuGet package ID '{nuGetId}'.");
    }
}
