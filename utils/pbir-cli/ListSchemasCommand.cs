// Copyright (c) 2024 navidata.io Corp

namespace FabricTools.Items.Report.Cli;

internal sealed class ListSchemasCommand : Command
{

    public override int Execute(CommandContext context)
    {
        var schemas = PbirValidator.Schemas;
        foreach (string docType in schemas.DocTypes)
        {
            AnsiConsole.MarkupLine($"[bold]{docType}[/]");
            foreach (var version in schemas.Versions(docType))
            {
                var schema = schemas[docType, version];
                AnsiConsole.MarkupLineInterpolated($" <{version}>: [blue]{schema.Uri}[/]");
            }
        }

        return 0;
    }
}