using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Steeltoe.Common.Utils.Diagnostics;
using Steeltoe.NetCoreToolService.Controllers;
using Steeltoe.NetCoreToolService.Models;
using Xunit;

namespace Steeltoe.NetCoreToolService.Test.Controllers
{
    public class NewControllerTest

    {
        /* ----------------------------------------------------------------- *
         * positive tests                                                    *
         * ----------------------------------------------------------------- */

        [Fact]
        public async Task GetTemplates_Should_Return_AllTemplates()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(expression: c => c.ExecuteAsync($"{NetCoreTool.Command} new --list", null, -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
-------------------  --------  ---------  ---------
My Template          myt       lang       tags
My Other Template    myot      otherlang  othertags
",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplates();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var templates = Assert.IsType<TemplateDictionary>(okResult.Value);
            templates.Count.Should().Be(2);
            templates.Keys.Should().Contain("myt");
            templates["myt"].Name.Should().Be("My Template");
            templates["myt"].Languages.Should().Be("lang");
            templates["myt"].Tags.Should().Be("tags");
            templates.Keys.Should().Contain("myot");
            templates["myot"].Name.Should().Be("My Other Template");
            templates["myot"].Languages.Should().Be("otherlang");
            templates["myot"].Tags.Should().Be("othertags");
        }

        [Fact]
        public async Task InstallTemplates_Should_Return_InstalledTemplates()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.SetupSequence(c => c.ExecuteAsync($"{NetCoreTool.Command} new --list", null, -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
-------------------  --------  ---------  ---------
My Template          myt       lang       tags
My Other Template    myot      otherlang  othertags
",
                    }
                )
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
-------------------  --------  ---------  ----------
A New Template       ant       smalltalk  newstuff
My Template          myt       lang       tags
My Other Template    myot      otherlang  othertags
Other New Template   ont       bigtalk    otherstuff
",
                    }
                );
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new --install My.Templates", null, -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = "",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.InstallTemplates("My.Templates");

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var templates = Assert.IsType<TemplateDictionary>(createdResult.Value);
            templates.Count.Should().Be(2);
            templates["ant"].Name.Should().Be("A New Template");
            templates["ant"].Languages.Should().Be("smalltalk");
            templates["ant"].Tags.Should().Be("newstuff");
            templates["ont"].Name.Should().Be("Other New Template");
            templates["ont"].Languages.Should().Be("bigtalk");
            templates["ont"].Tags.Should().Be("otherstuff");
        }

        [Fact]
        public async Task UninstallTemplates_Should_Return_UninstalledTemplates()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.SetupSequence(c => c.ExecuteAsync($"{NetCoreTool.Command} new --list", null, -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
-------------------  --------  ---------  ----------
A New Template       ant       smalltalk  newstuff
My Template          myt       lang       tags
My Other Template    myot      otherlang  othertags
Other New Template   ont       bigtalk    otherstuff
",
                    }
                )
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
-------------------  --------  ---------  ---------
My Template          myt       lang       tags
My Other Template    myot      otherlang  othertags
",
                    }
                );
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new --uninstall My.Templates", null, -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = "",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.UninstallTemplates("My.Templates");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var templates = Assert.IsType<TemplateDictionary>(ok.Value);
            templates.Count.Should().Be(2);
            templates["ant"].Name.Should().Be("A New Template");
            templates["ant"].Languages.Should().Be("smalltalk");
            templates["ant"].Tags.Should().Be("newstuff");
            templates["ont"].Name.Should().Be("Other New Template");
            templates["ont"].Languages.Should().Be("bigtalk");
            templates["ont"].Tags.Should().Be("otherstuff");
        }

        [Fact]
        public async Task GetTemplateHelp_Should_Return_Help()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new mytemplate --help", null, -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
Some helpful tips for you
",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateHelp("mytemplate");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var help = Assert.IsType<string>(ok.Value);
            help.Should().Be("Some helpful tips for you");
        }

        [Fact]
        public async Task GetTemplateProject_Should_Return_ProjectPackage()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new mytemplate --output=Sample",
                    It.IsAny<string>(), -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
The template ""mytemplate"" was created successfully.
",
                        Error = "",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateProject("mytemplate");

            // Assert
            Assert.IsType<FileContentResult>(result);
        }

        [Fact]
        public async Task GetTemplateProject_Should_Use_Defaults()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new mytemplate --output=Sample",
                    It.IsAny<string>(), -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
The template ""mytemplate"" was created successfully.
",
                        Error = "",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateProject("mytemplate");

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
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new mytemplate --output=Joe",
                    It.IsAny<string>(), -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
The template ""mytemplate"" was created successfully.
",
                        Error = "",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateProject("mytemplate", "output=Joe");

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            file.FileDownloadName.Should().StartWith("Joe.");
        }

        [Fact]
        public async Task GetTemplateProject_Can_Specify_ZipPackaging()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new mytemplate --output=Sample",
                    It.IsAny<string>(), -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
The template ""mytemplate"" was created successfully.
",
                        Error = "",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateProject("mytemplate", packaging: "zip");

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            var _ = new ZipArchive(new MemoryStream(file.FileContents));
        }

        /* ----------------------------------------------------------------- *
         * negative tests                                                    *
         * ----------------------------------------------------------------- */

        [Fact]
        public async Task InstallTemplates_UnknownNuGet_Should_Return_BadRequest()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new --list", null, -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
-------------------  --------  ---------  ---------
My Template          myt       lang       tags
My Other Template    myot      otherlang  othertags
",
                    }
                );
            executor.Setup(c =>
                    c.ExecuteAsync($"{NetCoreTool.Command} new --install No.Such.Template", null, -1))
                .ReturnsAsync(new CommandResult
                {
                    ExitCode = 2,
                    Output = @"
... error NU1101: Unable to find package No.Such.Template. No packages exist with this id in source(s): myget.org, nuget.org
Failed to restore ...
",
                });

            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.InstallTemplates("No.Such.Template");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            badRequest.Value.Should()
                .Be(
                    "Unable to find package No.Such.Template. No packages exist with this id in source(s): myget.org, nuget.org");
        }

        [Fact]
        public async Task UninstallTemplates_UnknownNuGet_Should_Return_NotFound()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new --list", null, -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
-------------------  --------  ---------  ---------
",
                    }
                );
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new --uninstall My.Templates", null, -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
Could not find something to uninstall called 'My.Templates'.
",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.UninstallTemplates("My.Templates");

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            notFound.Value.Should().Be("No templates with NuGet ID 'My.Templates' installed.");
        }

        [Fact]
        public async Task GetTemplateHelp_UnknownTemplate_Should_Return_NotFound()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c => c.ExecuteAsync($"{NetCoreTool.Command} new nosuchtemplate --help", null, -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 6,
                        Error = @"
No templates found matching: 'nosuchtemplate'.
",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateHelp("nosuchtemplate");

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            notFound.Value.Should().Be("No templates found matching: 'nosuchtemplate'.");
        }

        [Fact]
        public async Task GetTemplateProject_UnknownTemplate_Should_Return_NotFound()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c =>
                    c.ExecuteAsync($"{NetCoreTool.Command} new nosuchtemplate --output=Sample", It.IsAny<string>(), -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 14,
                        Error = @"
No templates found matching: 'nosuchtemplate'.
",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateProject("nosuchtemplate");

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            notFound.Value.Should().Be("Template 'nosuchtemplate' not found.");
        }

        [Fact]
        public async Task GetTemplateProject_UnknownSwitch_Should_Return_NotFound()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c =>
                    c.ExecuteAsync($"{NetCoreTool.Command} new mytemplate --output=Sample --unknown-switch",
                        It.IsAny<string>(), -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 5,
                        Error = @"
Invalid input switch:
  --unknown-switch
For a list of valid options, run 'dotnet new webapi --help'.
",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateProject("mytemplate", "unknown-switch");

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            notFound.Value.Should().Be("Switch 'unknown-switch' not found.");
        }

        [Fact]
        public async Task GetTemplateProject_UnknownParameter_Should_Return_NotFound()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c =>
                    c.ExecuteAsync($"{NetCoreTool.Command} new mytemplate --output=Sample --myoption=unknown",
                        It.IsAny<string>(), -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = "",
                        Error = @"
Error: Invalid parameter(s):
--myoption unknown
    'unknown' is not a valid value for --myoption
",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateProject("mytemplate", "myoption=unknown");

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            notFound.Value.Should().Be("Option 'myoption' parameter 'unknown' not found.");
        }

        [Fact]
        public async Task GetTemplateProject_UnknownPackaging_Should_Return_BadRequest()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c =>
                    c.ExecuteAsync($"{NetCoreTool.Command} new mytemplate --output=Sample --myoption=unknown",
                        It.IsAny<string>(), -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = "",
                        Error = @"
Error: Invalid parameter(s):
--myoption unknown
    'unknown' is not a valid value for --myoption
",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateProject("mytemplate", packaging: "acme-packaging");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            badRequest.Value.Should().Be("Unknown or unsupported packaging 'acme-packaging'.");
        }

        [Fact]
        public async Task GetTemplateProject_UnknownError_Should_Return_InternalServerError()
        {
            // Arrange
            var executor = new Mock<ICommandExecutor>();
            executor.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 1,
                        Output = "",
                        Error = @"
Something bad happened.
",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateProject("mytemplate");

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
            executor.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), -1))
                .ReturnsAsync(new CommandResult
                    {
                        ExitCode = 0,
                        Output = @"
Unexpected output.
",
                        Error = "",
                    }
                );
            var controller = new NewController(executor.Object);

            // Act
            var result = await controller.GetTemplateProject("mytemplate");

            // Assert
            var internalServerError = Assert.IsType<ObjectResult>(result);
            internalServerError.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            internalServerError.Value.Should().Be("Unexpected output.");
        }
    }
}
