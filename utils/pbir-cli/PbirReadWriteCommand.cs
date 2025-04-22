// Copyright (c) 2024 navidata.io Corp

using Newtonsoft.Json;

#pragma warning disable IO0007

namespace FabricTools.Items.Report.Cli;

internal sealed class PbirReadWriteCommand : FileSystemCommand<PbirReadWriteCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Input path to read PBIR definition from.")]
        [CommandArgument(0, "<inputPath>")]
        public required string InputPath { get; init; }

        [Description("Output path for definition files.")]
        [CommandArgument(1, "<outputPath>")]
        public required string OutputPath { get; init; }
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (!FileSystem.Directory.Exists(settings.InputPath))
            return ValidationResult.Error($"The input path '{settings.InputPath}' does not exist.");
        if (FileSystem.File.Exists(settings.OutputPath))
            return ValidationResult.Error($"The output path '{settings.OutputPath}' is a file.");

        return ValidationResult.Success();
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine("[blue]Input Path:[/] {0}", settings.InputPath);
        AnsiConsole.MarkupLine("[blue]Output Path:[/] {0}", settings.OutputPath);

        var pbir = PbirDefinition.FromPath(settings.InputPath);
        var results = pbir.Write(settings.OutputPath, serializer =>
            serializer.Formatting = Formatting.Indented
        );

        foreach (var writtenPath in results.FilesWritten)
        {
            AnsiConsole.WriteLine("Written: {0}", writtenPath);
        }
        foreach (var deletedPath in results.FilesDeleted)
        {
            AnsiConsole.WriteLine("Deleted: {0}", deletedPath);
        }
        return 0;
    }

}