using System.IO.Abstractions.TestingHelpers;
using FabricTools.Items.IO;
using FabricTools.Items.Report.Definitions;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;

namespace FabricTools.Items.Report.Tests;

public class JsonReaderTests : HasTestFolder
{
    // ReSharper disable once InconsistentNaming
    private readonly AssemblyResources Resources = new();

    [Fact]
    public void ReadBookmark()
    {
        var resource = Resources["Bookmarkd1892fa34230d040162b.bookmark.json"];
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            {@"C:\report\bookmark.json", new MockFileData(resource.GetString())}
        });

        var traceWriter = new MemoryTraceWriter(); // for debugging only

        var reader = new PbirDefinitionReader(
            new DefaultFileSystem(fileSystem, @"C:\report"),
            NullLoggerFactory.Instance,
            _ => traceWriter);

        Bookmark result = null!;
        reader.ReadFile<Bookmark>(RelativeFilePath.Empty, "bookmark.json",
            bookmark => result = bookmark);

        Assert.NotNull(result);
    }
}