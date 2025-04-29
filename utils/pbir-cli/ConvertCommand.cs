// Copyright (c) 2024 navidata.io Corp

using FabricTools.Items.Report.Conversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FabricTools.Items.Report.Cli;

internal sealed class PbirConvertCommand : FileSystemCommand<PbirConvertCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Input path to read PBIR definition from. Must be the 'definition/' folder of a report item.")]
        [CommandArgument(0, "<inputPath>")]
        public required string InputPath { get; init; }

        [Description("Output directory. Will be created if it does not exist and cleared if it does exist.")]
        [CommandArgument(1, "<outputPath>")]
        public required string OutputPath { get; init; }

        [Description("Exports all object/config/filters parts into separate files, creating subfolders for pages and visuals instead of writing the legacy single 'report.json' output.")]
        [CommandOption("--expanded")]
        public bool Expanded { get; init; } = false;
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
        var inputDir = FileSystem.DirectoryInfo.New(settings.InputPath);
        AnsiConsole.MarkupLineInterpolated($"[blue]Reading PBIR Definition from:[/] {inputDir.FullName}");

        var pbirDefinition = PbirDefinition.FromPath(inputDir.FullName);

        var outputDir = FileSystem.DirectoryInfo.New(settings.OutputPath);
        outputDir.Create();
#pragma warning disable IO0007
        new DirectoryInfo(outputDir.FullName).Clean(keepDotFolders: true, keepDotFiles: true);
#pragma warning restore IO0007

        IFileInfo File(params string[] paths) =>
            FileSystem.FileInfo.New(FileSystem.Path.Combine([outputDir.FullName, ..paths]));

        var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
        var v1Report = ReportFormatConverter.ConvertToV1Report(pbirDefinition);

        if (settings.Expanded)
        {
            WriteJson(v1Report.Report.Base, File("report.json"), serializer);
            WriteJson(v1Report.Report.Config, File("report.config.json"), serializer);
            WriteJson(v1Report.Report.Filters, File("report.filters.json"), serializer);
            foreach (var page in v1Report.Report.Children ?? [])
            {
                var pageName = page.Base["name"]!.ToString();
                WriteJson(page.Base, File("pages", pageName, "page.json"), serializer);
                WriteJson(page.Config, File("pages", pageName, "page.config.json"), serializer);
                WriteJson(page.Filters, File("pages", pageName, "page.filters.json"), serializer);

                foreach (var visual in page.Children ?? [])
                {
                    var visualName = visual.Config["name"]!.ToString();
                    WriteJson(visual.Base, File("pages", pageName, "visuals", visualName, "visual.json"), serializer);
                    WriteJson(visual.Config, File("pages", pageName, "visuals", visualName, "visual.config.json"), serializer);
                    WriteJson(visual.Filters, File("pages", pageName, "visuals", visualName, "visual.filters.json"), serializer);
                }
            }
        }
        else
        {
            var reportJson = v1Report.Export();
            WriteJson(reportJson, File("report.json"), serializer);
        }

        return 0;
    }

    private static void WriteJson(JToken? json, IFileInfo fileInfo, JsonSerializer jsonSerializer)
    {
        if (json is null) return;

        fileInfo.Directory?.Create();

        AnsiConsole.WriteLine(fileInfo.FullName);
        using var jsonWriter = new JsonTextWriter(fileInfo.CreateText());
        jsonSerializer.Serialize(jsonWriter, json);
    }

}