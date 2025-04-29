// Copyright (c) 2024 navidata.io Corp

using FabricTools.Items.Report.Schemas;
using Newtonsoft.Json;
using DirectoryInfoWrapper = Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper;

namespace FabricTools.Items.Report.Cli;

internal sealed class PbirValidateCommand : FileSystemCommand<PbirValidateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Input path to read PBIR definition from.")]
        [CommandArgument(0, "<inputPath>")]
        public required string InputPath { get; init; }

        internal DirectoryInfo InputDirectoryInfo = null!;

        [CommandOption("--allow-online-schema-load"),
         DefaultValue(false),
         Description("Allows loading of schemas from the url at a file's '$schema' property")]
        public bool AllowOnlineSchemaLoad { get; set; } = false;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
#pragma warning disable IO0007
        if (!(settings.InputDirectoryInfo = new DirectoryInfo(settings.InputPath)).Exists)
#pragma warning restore IO0007
            return ValidationResult.Error($"The input directory '{settings.InputPath}' does not exist.");

        return ValidationResult.Success();
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLineInterpolated($"[blue]Input Path:[/] {settings.InputPath}");

        var pbirValidator = new PbirValidator { AllowOnlineSchemaLoad = settings.AllowOnlineSchemaLoad };
        var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher().AddInclude("**/*.json");
        var matchResult = matcher.Execute(new DirectoryInfoWrapper(settings.InputDirectoryInfo));

        foreach (var file in matchResult.Files)
        {
            AnsiConsole.MarkupLineInterpolated($"Processing file: [bold]{file.Path}[/]");
            var fullPath = FileSystem.Path.Combine(settings.InputDirectoryInfo.FullName, file.Path);
            using (var reader = new JsonTextReader(FileSystem.File.OpenText(fullPath)))
            {
                if (reader.ReadStringProperty(ItemSchemas.SchemaProperty) is { } schema)
                {
                    AnsiConsole.MarkupLineInterpolated($"> Schema: [grey]{schema}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"Skipping file since no '{ItemSchemas.SchemaProperty}' property is present.");
                    continue;
                }
            }

            var noErrors = true;
            foreach (var error in pbirValidator.Validate(fullPath))
            {
                noErrors = false;
                AnsiConsole.MarkupLineInterpolated($"[red]Validation Error: {error}[/]");
                if (error.HasLineInfo)
                {
                    AnsiConsole.MarkupLineInterpolated($"[yellow]Line number: {error.LineNumber}, position: {error.LinePosition}[/]");
                }
            }

            if (noErrors)
                AnsiConsole.MarkupLine("[green]No validation errors.[/]");
        }

        return 0;
    }

}