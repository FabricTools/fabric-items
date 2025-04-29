// Copyright (c) 2024 navidata.io Corp

using FabricTools.Items.IO;
using Microsoft.Extensions.Logging;

namespace FabricTools.Items.Report.Cli;

internal sealed class PbirReadDebugCommand : FileSystemCommand<PbirReadDebugCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Input path to read PBIR definition from.")]
        [CommandArgument(0, "<inputPath>")]
        public required string InputPath { get; init; }
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (!FileSystem.Directory.Exists(settings.InputPath))
            return ValidationResult.Error($"The input path '{settings.InputPath}' does not exist.");
        return ValidationResult.Success();
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.Debug)
            .AddSimpleConsole());

        AnsiConsole.MarkupLine("[blue]Input Path:[/] {0}", settings.InputPath);

        var reader = new PbirDefinitionReader(
            new DefaultFileSystem(new FileSystem(), settings.InputPath, loggerFactory), 
            loggerFactory, 
            enableTracing: true
        );
        var pbir = reader.Read();

        AnsiConsole.MarkupLine("[blue]PBIR Definition read successfully.[/]");

        return 0;
    }

}