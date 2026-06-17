// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.NetCoreToolService.Controllers;
using Steeltoe.NetCoreToolService.Models;
using Steeltoe.NetCoreToolService.SteeltoeUtils.Diagnostics;
using Xunit;

namespace Steeltoe.NetCoreToolService.Test.Controllers;

public sealed class NewControllerTest
{
    [Fact]
    public async Task GetTemplates_Should_Return_AllTemplates()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new list", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = """
                -------------------  --------  ----------  ----------
                My Template          myt       lang        tags
                My Other Template    my-ot     other-lang  other-tags
                """
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplates();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var templates = Assert.IsType<TemplateDictionary>(okResult.Value);
        templates.Count.Should().Be(2);
        templates.Keys.Should().Contain("myt");
        templates["myt"].Name.Should().Be("My Template");
        templates["myt"].Languages.Should().Be("lang");
        templates["myt"].Tags.Should().Be("tags");
        templates.Keys.Should().Contain("my-ot");
        templates["my-ot"].Name.Should().Be("My Other Template");
        templates["my-ot"].Languages.Should().Be("other-lang");
        templates["my-ot"].Tags.Should().Be("other-tags");
    }

    [Fact]
    public async Task InstallTemplates_Should_Return_InstalledTemplates()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.SetupSequence(c => c.ExecuteAsync($"{NetCoreTool.Command} new list", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = """
                -------------------  --------  ----------  ----------
                My Template          myt       lang        tags
                My Other Template    my-ot     other-lang  other-tags
                """
        }).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = """
                -------------------  --------  ----------  -----------
                A New Template       ant       smalltalk   new-stuff
                My Template          myt       lang        tags
                My Other Template    my-ot     other-lang  other-tags
                Other New Template   ont       big-talk    other-stuff
                """
        });

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new install My.Templates", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = string.Empty
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.InstallTemplates("My.Templates");

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var templates = Assert.IsType<TemplateDictionary>(createdResult.Value);
        templates.Count.Should().Be(2);
        templates["ant"].Name.Should().Be("A New Template");
        templates["ant"].Languages.Should().Be("smalltalk");
        templates["ant"].Tags.Should().Be("new-stuff");
        templates["ont"].Name.Should().Be("Other New Template");
        templates["ont"].Languages.Should().Be("big-talk");
        templates["ont"].Tags.Should().Be("other-stuff");
    }

    [Fact]
    public async Task UninstallTemplates_Should_Return_UninstalledTemplates()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.SetupSequence(c => c.ExecuteAsync($"{NetCoreTool.Command} new list", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = """
                -------------------  --------  ----------  -----------
                A New Template       ant       smalltalk   new-stuff
                My Template          myt       lang        tags
                My Other Template    my-ot     other-lang  other-tags
                Other New Template   ont       big-talk    other-stuff
                """
        }).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = """
                -------------------  --------  ----------  ----------
                My Template          myt       lang        tags
                My Other Template    my-ot     other-lang  other-tags
                """
        });

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new uninstall My.Templates", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = string.Empty
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.UninstallTemplates("My.Templates");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var templates = Assert.IsType<TemplateDictionary>(ok.Value);
        templates.Count.Should().Be(2);
        templates["ant"].Name.Should().Be("A New Template");
        templates["ant"].Languages.Should().Be("smalltalk");
        templates["ant"].Tags.Should().Be("new-stuff");
        templates["ont"].Name.Should().Be("Other New Template");
        templates["ont"].Languages.Should().Be("big-talk");
        templates["ont"].Tags.Should().Be("other-stuff");
    }

    [Fact]
    public async Task GetTemplateHelp_Should_Return_Help()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new my-template --help", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = "Some helpful tips for you"
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateHelp("my-template");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        string help = Assert.IsType<string>(ok.Value);
        help.Should().Be("Some helpful tips for you");
    }

    [Fact]
    public async Task GetTemplateProject_Should_Return_ProjectPackage()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new my-template --output=Sample", It.IsAny<string>(), -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = """
                The template "my-template" was created successfully.
                """,
            Error = string.Empty
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template");

        // Assert
        Assert.IsType<FileContentResult>(result);
    }

    [Fact]
    public async Task GetTemplateProject_Should_Use_Defaults()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new my-template --output=Sample", It.IsAny<string>(), -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = """
                The template "my-template" was created successfully.
                """,
            Error = string.Empty
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template");

        // Assert
        var file = Assert.IsType<FileContentResult>(result);
        file.ContentType.Should().Be("application/zip");
        file.FileDownloadName.Should().Be("Sample.zip");
    }

    [Fact]
    public async Task GetTemplateProject_Can_Specify_Output()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new my-template --output=Joe", It.IsAny<string>(), -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = """
                The template "my-template" was created successfully.
                """,
            Error = string.Empty
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template", "output=Joe");

        // Assert
        var file = Assert.IsType<FileContentResult>(result);
        file.FileDownloadName.Should().StartWith("Joe.");
    }

    [Fact]
    public async Task GetTemplateProject_Can_Specify_ZipPackaging()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new my-template --output=Sample", It.IsAny<string>(), -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = """
                The template "my-template" was created successfully.
                """,
            Error = string.Empty
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template", packaging: "zip");

        // Assert
        var file = Assert.IsType<FileContentResult>(result);
        _ = new ZipArchive(new MemoryStream(file.FileContents));
    }

    [Fact]
    public async Task InstallTemplates_UnknownNuGet_Should_Return_BadRequest()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new list", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = """
                -------------------  --------  ----------  ----------
                My Template          myt       lang        tags
                My Other Template    my-ot     other-lang  other-tags
                """
        });

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new install No.Such.Template", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 2,
            Output = """
                ... error NU1101: Unable to find package No.Such.Template. No packages exist with this id in source(s): myget.org, nuget.org
                Failed to restore ...
                """
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.InstallTemplates("No.Such.Template");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        badRequest.Value.Should().Be("Unable to find package No.Such.Template. No packages exist with this id in source(s): myget.org, nuget.org");
    }

    [Fact]
    public async Task UninstallTemplates_UnknownNuGet_Should_Return_NotFound()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new list", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = "-------------------  --------  ---------  ---------"
        });

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new uninstall My.Templates", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = "Could not find something to uninstall called 'My.Templates'."
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.UninstallTemplates("My.Templates");

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        notFound.Value.Should().Be("No templates with NuGet ID 'My.Templates' installed.");
    }

    [Fact]
    public async Task GetTemplateHelp_UnknownTemplate_Should_Return_NotFound()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new no-such-template --help", null, -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 6,
            Error = "No templates found matching: 'no-such-template'."
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateHelp("no-such-template");

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        notFound.Value.Should().Be("No templates found matching: 'no-such-template'.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownTemplate_Should_Return_NotFound()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new no-such-template --output=Sample", It.IsAny<string>(), -1)).ReturnsAsync(
            new CommandResult
            {
                ExitCode = 14,
                Error = "No templates found matching: 'no-such-template'."
            });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("no-such-template");

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        notFound.Value.Should().Be("Template 'no-such-template' not found.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownSwitch1_Should_Return_NotFound()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new my-template --output=Sample --unknown-switch", It.IsAny<string>(), -1)).ReturnsAsync(
            new CommandResult
            {
                ExitCode = 5,
                Error = """
                    Invalid input switch:
                      --unknown-switch
                    For a list of valid options, run 'dotnet new webapi --help'.
                    """
            });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template", "unknown-switch");

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        notFound.Value.Should().Be("Switch 'unknown-switch' not found.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownSwitch2_Should_Return_NotFound()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new my-template --output=Sample --unknown-switch", It.IsAny<string>(), -1)).ReturnsAsync(
            new CommandResult
            {
                ExitCode = 5,
                Error = """
                    Error: Invalid option(s):
                    --unknown-switch
                       '--unknown-switch' is not a valid option
                    """
            });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template", "unknown-switch");

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        notFound.Value.Should().Be("Switch 'unknown-switch' not found.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownParameter_Should_Return_NotFound()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new my-template --output=Sample --my-option=unknown", It.IsAny<string>(), -1)).ReturnsAsync(
            new CommandResult
            {
                ExitCode = 0,
                Output = string.Empty,
                Error = """
                    Error: Invalid parameter(s):
                    --my-option unknown
                        'unknown' is not a valid value for --my-option
                    """
            });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template", "my-option=unknown");

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        notFound.Value.Should().Be("Option 'my-option' parameter 'unknown' not found.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownPackaging_Should_Return_BadRequest()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new my-template --output=Sample --my-option=unknown", It.IsAny<string>(), -1)).ReturnsAsync(
            new CommandResult
            {
                ExitCode = 0,
                Output = string.Empty,
                Error = """
                    Error: Invalid parameter(s):
                    --my-option unknown
                        'unknown' is not a valid value for --my-option
                    """
            });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template", packaging: "acme-packaging");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        badRequest.Value.Should().Be("Unknown or unsupported packaging 'acme-packaging'.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownError_Should_Return_InternalServerError()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 1,
            Output = string.Empty,
            Error = "Something bad happened."
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template");

        // Assert
        var internalServerError = Assert.IsType<ObjectResult>(result);
        internalServerError.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        internalServerError.Value.Should().Be("Something bad happened.");
    }

    [Fact]
    public async Task GetTemplateProject_UnknownOutput_Should_Return_InternalServerError()
    {
        // Arrange
        var executor = new Mock<ICommandExecutor>();

        executor.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), -1)).ReturnsAsync(new CommandResult
        {
            ExitCode = 0,
            Output = "Unexpected output.",
            Error = string.Empty
        });

        var controller = new NewController(executor.Object, NullLogger<NewController>.Instance);

        // Act
        ActionResult result = await controller.GetTemplateProject("my-template");

        // Assert
        var internalServerError = Assert.IsType<ObjectResult>(result);
        internalServerError.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        internalServerError.Value.Should().Be("Unexpected output.");
    }
}
