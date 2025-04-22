// Copyright (c) 2024 navidata.io Corp

using System.IO.Abstractions;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FabricTools.Items.IO;

// ReSharper disable once CheckNamespace
namespace FabricTools.Items.Report.Tests;

public class E2EReaderTests(ITestOutputHelper output)
    : LoggingBase(output.ToLoggerFactory())
{

    public static TheoryData<string> Names { get; } =
    [
        "AdventureWorks Sales.Report",
        "Competitive Marketing Analysis.Report",
        "Corporate Spend.Report",
        "Employee Hiring and History.Report",
        "Regional Sales Sample.Report",
        "Store Sales.Report",
    ];

    private static readonly PbirValidator validator = new() { AllowOnlineSchemaLoad = false};

    [Theory]
    [MemberData(nameof(Names))]
    public async Task Can_read_PBIR_folder(string folderName)
    {
        using var tempFolder = new TempFolder();
        var resources = new AssemblyResources();

        foreach (var item in resources.StartsWith(folderName))
        {
            await item.ExtractToAsync(tempFolder.Path);
        }

        var definitionPath = new DirectoryInfo(tempFolder.Path)
            .EnumerateDirectories()
            .First()
            .EnumerateDirectories("definition")
            .Single()
            .FullName;

        var def = PbirDefinition.FromPath(definitionPath);
        Assert.NotNull(def);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public async Task Can_write_PBIR_folder(string folderName)
    {
        using var tempFolder = new TempFolder();
        var resources = new AssemblyResources();

        var originalDir = new DirectoryInfo(tempFolder.Path).CreateSubdirectory("input");
        var writtenDir = new DirectoryInfo(tempFolder.Path).CreateSubdirectory("output");

        foreach (var item in resources.StartsWith(folderName))
        {
            await item.ExtractToAsync(originalDir.FullName);
        }

        var definitionPath = originalDir
            .EnumerateDirectories()
            .First()
            .EnumerateDirectories("definition")
            .Single()
            .FullName;

        var def = PbirDefinition.FromPath(definitionPath);

        def.Write(writtenDir.FullName);
        Assert.True(writtenDir.EnumerateFiles("*.json", SearchOption.AllDirectories).Any());
    }

    [Theory(Skip = "Ignore whilst we cannot produce an exact match - Use schema validation instead")]
    [MemberData(nameof(Names))]
    public async Task Write_output_matches_original_input(string folderName)
    {
        // Extract the embedded PBIR folder
        // Read as PbirDefinition
        // Write to a new folder
        // Compare all corresponding files, expecting a full match for each

        using var tempFolder = new TempFolder();
        var resources = new AssemblyResources();

        var originalDir = new DirectoryInfo(tempFolder.Path).CreateSubdirectory("input");
        var writtenDir = new DirectoryInfo(tempFolder.Path).CreateSubdirectory("output");

        foreach (var item in resources.StartsWith(folderName))
        {
            await item.ExtractToAsync(originalDir.FullName);
        }

        var definitionPath = originalDir
            .EnumerateDirectories()
            .First()
            .EnumerateDirectories("definition")
            .Single()
            .FullName;

        var def = PbirDefinition.FromPath(definitionPath);

        def.Write(writtenDir.FullName);

        var inputFileSystem = new DefaultFileSystem(new FileSystem(), definitionPath);
        var outputFileSystem = new DefaultFileSystem(new FileSystem(), writtenDir.FullName);

        var inputFiles =
            new HashSet<RelativeFilePath>(inputFileSystem.EnumerateFiles("*.*",
                searchOption: SearchOption.AllDirectories));
        var outputFiles = new HashSet<RelativeFilePath>(outputFileSystem.EnumerateFiles("*.*",
            searchOption: SearchOption.AllDirectories));

        if (!inputFiles.SetEquals(outputFiles))
        {
            var missingFiles = inputFiles.Except(outputFiles).ToArray();
            var extraFiles = outputFiles.Except(inputFiles).ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("Missing files:");
            foreach (var file in missingFiles)
            {
                sb.AppendLine(file.ToString());
            }

            sb.AppendLine("Extra files:");
            foreach (var file in extraFiles)
            {
                sb.AppendLine(file.ToString());
            }

            Assert.Fail(sb.ToString());
        }

        foreach (var filePath in inputFiles)
        {
#if NET
            await 
#endif
            using var inputReader = new JsonTextReader(inputFileSystem.CreateTextReader(filePath));
#if NET
            await 
#endif
            using var outputReader = new JsonTextReader(outputFileSystem.CreateTextReader(filePath));

            var inputJson = await JObject.LoadAsync(inputReader, TestContext.Current.CancellationToken);
            var outputJson = await JObject.LoadAsync(outputReader, TestContext.Current.CancellationToken);

            if (JToken.DeepEquals(inputJson, outputJson))
                continue;

            var sb = new StringBuilder();
            sb.AppendLine($"File {filePath} does not match.");
            sb.AppendLine();
            sb.AppendLine("Original:");
            sb.AppendLine(inputJson.ToString(Formatting.Indented));
            sb.AppendLine();
            sb.AppendLine("Written:");
            sb.AppendLine(outputJson.ToString(Formatting.Indented));

            Assert.Fail(sb.ToString());
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Written_output_validates_against_schemas(string folderName)
    {
        using var fixture = new ReadWriteFixture(folderName, LoggerFactory);

        var errorOutput = new StringBuilder();

        foreach (var file in fixture.OutputDirectory.EnumerateFiles("*.json", SearchOption.AllDirectories))
        {
            Logger.LogInformation("Validating file at: {Path}", file.FullName);
            var errors = validator.Validate(file.FullName);

            if (errors.Count == 0)
                continue;

            errorOutput.AppendLine($"Errors in {file.FullName}:");
            foreach (var error in errors)
            {
                errorOutput.AppendLine(error.ToString());
            }
        }

        if (errorOutput.Length > 0)
        {
            Assert.Fail(errorOutput.ToString());
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Input_files_validate_against_schemas(string folderName)
    {
        using var fixture = new ReadWriteFixture(folderName);

        var errorOutput = new StringBuilder();

        foreach (var file in fixture.InputDirectory.EnumerateFiles("*.json", SearchOption.AllDirectories))
        {
            var errors = validator.Validate(file.FullName);

            if (errors.Count == 0)
                continue;

            errorOutput.AppendLine($"Errors in {file.FullName}:");
            foreach (var error in errors)
            {
                errorOutput.AppendLine(error.ToString());
            }
        }

        if (errorOutput.Length > 0)
        {
            Assert.Fail(errorOutput.ToString());
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Input_and_output_folders_contain_same_number_of_files(string folderName)
    {
        using var fixture = new ReadWriteFixture(folderName, LoggerFactory);
        Assert.Equal(
            fixture.InputDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories).Count(),
            fixture.OutputDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories).Count());
    }

}