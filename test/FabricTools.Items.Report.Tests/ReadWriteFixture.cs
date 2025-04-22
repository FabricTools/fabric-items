// Copyright (c) 2024 navidata.io Corp

using Microsoft.Extensions.Logging.Abstractions;

namespace FabricTools.Items.Report.Tests;

/// <summary>
/// Extracts embedded PBIR resources to a temporary folder, and reads them as a <see cref="PbirDefinition"/>.
/// Then writes the <see cref="PbirDefinition"/> to a new folder.
/// </summary>
internal class ReadWriteFixture : HasTestFolder
{
    public ReadWriteFixture(string folderName, ILoggerFactory loggerFactory = null)
    {
        loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        var logger = loggerFactory.CreateLogger<ReadWriteFixture>();

        var resources = new AssemblyResources();

        var input = new DirectoryInfo(TestFolder.Path).CreateSubdirectory("input");

        foreach (var item in resources.StartsWith(folderName))
        {
            var path = item.ExtractToAsync(input.FullName).Result;
            logger.LogDebug("Writing file to {Path}", path);
        }

        InputDirectory = input                              // ./input
            .EnumerateDirectories().First()                 // ./input/{NAME}.Report
            .EnumerateDirectories("definition").Single();   // ./input/{NAME}.Report/definition

        OriginalDefinition = PbirDefinition.FromPath(InputDirectory.FullName, loggerFactory);

        OutputDirectory = new DirectoryInfo(TestFolder.Path).CreateSubdirectory("output");
        WriteResults = OriginalDefinition.Write(OutputDirectory.FullName);
    }

    /// <summary>
    /// The <c>/definition</c> folder the original PBIR sources were extracted to.
    /// </summary>
    public DirectoryInfo InputDirectory { get; }

    /// <summary>
    /// The <c>/definition</c> folder the <see cref="PbirDefinition"/> was written to.
    /// </summary>
    public DirectoryInfo OutputDirectory { get; }

    /// <summary>
    /// The original <see cref="PbirDefinition"/> read from <see cref="InputDirectory"/>.
    /// </summary>
    public PbirDefinition OriginalDefinition { get; }

    public WriteOperationResults WriteResults { get; }
}