// Copyright (c) 2024 navidata.io Corp

using Newtonsoft.Json;

namespace FabricTools.Items.Report;

using IO;

/// <summary>
/// A report definition compliant with the <see href="https://fabric.onl/pbir">PBIR format</see>.
/// Represents all files within the <c>/definition</c> folder of a Fabric <c>Report</c> item.
/// </summary>
public class PbirDefinition
{
    /// <summary>
    /// The PBIR schema version.
    /// Typically declared in the <c>version.json</c> file.
    /// </summary>
    public Definitions.VersionMetadata VersionDocument { get; internal set; } = new();

    /// <summary>
    /// Report metadata, such as report level filters and formatting.
    /// Typically declared in the <c>report.json</c> file.
    /// </summary>
    public Definitions.Report Report { get; internal set; } = new();

    /// <summary>
    /// Defines all report extensions, such as report level measures.
    /// Typically declared inside the <c>reportExtensions.json/</c> file, although the file is not required.
    /// </summary>
    public Definitions.ReportExtension? ReportExtensions { get; set; }
    
    /// <summary>
    /// Defines all report bookmarks.
    /// Typically declared inside the <c>bookmarks/</c> folder.
    /// </summary>
    public IPbirDocumentCollection<Definitions.Bookmark, Definitions.BookmarksMetadata> Bookmarks { get; }
        = new PbirDocumentCollection<Definitions.Bookmark, Definitions.BookmarksMetadata>(PbirNames.BookmarksFolder);

    /// <summary>
    /// Defines all report pages.
    /// Typically declared inside the <c>pages/</c> folder.
    /// </summary>
    public IPbirDocumentCollection<Definitions.Page, Definitions.PagesMetadata> Pages { get; }
        = new PbirDocumentCollection<Definitions.Page, Definitions.PagesMetadata>(PbirNames.PagesFolder);

    #region Reader/Writer Methods

    /// <summary>
    /// Gets the file system this <see cref="PbirDefinition"/> was originally read from.
    /// Returns <c>null</c> if the definition was not read from a file system.
    /// </summary>
    public IFabricItemFileSystem? FileSystem { get; internal set; }

    /// <summary>
    /// Generates a <see cref="PbirDefinition"/> from the specified file system path.
    /// </summary>
    public static PbirDefinition FromPath(string path, ILoggerFactory? loggerFactory = null) =>
        new PbirDefinitionReader(
                new DefaultFileSystem(new System.IO.Abstractions.FileSystem(), path, loggerFactory))
            .Read();

    /// <summary>
    /// Writes this <see cref="PbirDefinition"/> to the specified file system path.
    /// If the path is omitted, the definition is written to the original file system it was read from.
    /// If the definition was not read from a file system, an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    /// <returns>The <see cref="WriteOperationResults"/> associated with the write operation.</returns>
    public WriteOperationResults Write(string? path = null
        , Action<JsonSerializer>? configureSerializer = null
        , ILoggerFactory? loggerFactory = null) => path switch
    {
        null when FileSystem is null => throw new InvalidOperationException("Cannot write a PBIR definition without a file system."),
        null => new PbirDefinitionWriter(
                this.FileSystem
                , configureSerializer
                , loggerFactory
            ).Write(this),
        _ => new PbirDefinitionWriter(
                new DefaultFileSystem(new System.IO.Abstractions.FileSystem(), path, loggerFactory)
                , configureSerializer
                , loggerFactory
            ).Write(this)
    };

    #endregion

}
