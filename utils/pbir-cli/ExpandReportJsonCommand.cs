// Copyright (c) 2024 navidata.io Corp

using FabricTools.Items.Report.Conversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FabricTools.Items.Report.Cli
{
    internal sealed class ExpandReportJsonCommand : FileSystemCommand<ExpandReportJsonCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Path to an existing 'report.json' file.")]
            [CommandArgument(0, "<inputPath>")]
            public required string InputPath { get; init; }
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            if (!FileSystem.File.Exists(settings.InputPath))
                return ValidationResult.Error($"The input path '{settings.InputPath}' does not exist.");

            return ValidationResult.Success();
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var sourceFile = FileSystem.FileInfo.New(settings.InputPath);
            AnsiConsole.MarkupLineInterpolated($"[blue]Reading report definition from:[/] {sourceFile.FullName}");

            JObject reportJson;
            using (var reader = new JsonTextReader(sourceFile.OpenText()))
            {
                reportJson = JObject.Load(reader);
            }

            var v1Report = V1ReportContainer.FromJson(reportJson);

            var outputDir = sourceFile.Directory!;
            IFileInfo File(params string[] paths) =>
                FileSystem.FileInfo.New(FileSystem.Path.Combine([outputDir.FullName, .. paths]));

            var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });

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
}