// Copyright (c) 2024 navidata.io Corp

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

    config.AddCommand<PbirValidateCommand>("validate")
        .WithDescription("Validates all schema-bound documents in the specified directory.")
        .WithExample("validate", "./report/Sales/Report/definition");

    config.AddCommand<ListSchemasCommand>("list-schemas")
        .WithDescription("Lists all known PBIR document schemas and their cached versions.");
});
try
{
    return app.Run(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return 1;
}