// Copyright (c) 2024 navidata.io Corp

using System.Diagnostics;

var app = new CommandApp();
app.Configure(config =>
{
    config
        .SetApplicationName("pbir-cli")
        .SetApplicationVersion(AssemblyVersionInformation.FromCallingAssembly().Version)
        .PropagateExceptions();

    config.AddCommand<PbirReadWriteCommand>("read-write")
        .WithDescription("Reads a PBIR definition from the input path and writes it to the output path.")
        .WithExample("read-write", "./reports/Sales.Report/definition", "./out/reports/Sales/definition");

    config.AddCommand<PbirReadDebugCommand>("read-debug")
        .IsHidden();

    config.AddCommand<PbirValidateCommand>("validate")
        .WithDescription("Validates all schema-bound documents in the specified directory.")
        .WithExample("validate", "./report/Sales/Report/definition");

    config.AddCommand<PbirConvertCommand>("convert")
        .WithDescription("Converts a PBIR definition to the report.json legacy format.")
        .WithExample("convert", "./report/Sales/Report/definition", "./report/Sales-v1")
        .WithExample("convert", "./report/Sales/Report/definition", "./report/Sales-v1", "--expanded");

    config.AddCommand<ExpandReportJsonCommand>("expand-report")
        .IsHidden();

    config.AddCommand<ListSchemasCommand>("list-schemas")
        .WithDescription("Lists all known PBIR document schemas and their cached versions.");
});
try
{
    return app.Run(args);
}
catch (Exception ex)
{
#if DEBUG
    if (Debugger.IsAttached)
        throw;
#endif
    AnsiConsole.WriteException(ex);
    return 1;
}