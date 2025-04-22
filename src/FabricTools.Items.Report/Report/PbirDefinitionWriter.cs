// Copyright (c) 2024 navidata.io Corp

using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using FabricTools.Items.IO;
using FabricTools.Items.Json;

namespace FabricTools.Items.Report;

/// <summary>
/// Writes PBIR definitions to a file system.
/// </summary>
public class PbirDefinitionWriter
{
    private readonly ILogger _logger;
    private readonly IFabricItemFileSystem _fileSystem;
    private readonly JsonSerializer _jsonSerializer;

    /// <summary>
    /// Creates a new instance of <see cref="PbirDefinitionWriter"/>.
    /// </summary>
    public PbirDefinitionWriter(IFabricItemFileSystem fileSystem
        , Action<JsonSerializer>? configureSerializer = null
        , ILoggerFactory? loggerFactory = default)
    {
        this._fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = loggerFactory?.CreateLogger<PbirDefinitionReader>() ?? NullLoggerFactory.Instance.CreateLogger<PbirDefinitionReader>();

        _jsonSerializer = new JsonSerializer { };
        _jsonSerializer.Converters.Add(new NullableAnyOfJsonConverter());
        _jsonSerializer.Converters.Add(new DoubleValueJsonConverter());
        _jsonSerializer.Converters.Add(new EnumJsonConverter());

        configureSerializer?.Invoke(_jsonSerializer);
    }

    /// <summary>
    /// If <c>True</c> (default), allows overwriting of existing files.
    /// Otherwise, a write operation will fail if the target directory is not empty.
    /// </summary>
    public bool Overwrite { get; set; } = true;

    /// <summary>
    /// If <c>True</c> (default), updates the schema id of each document before writing it.
    /// This is recommended to ensure that no accidental validation errors are introduced
    /// when writing newly introduced properties in combination with older schema versions.
    /// </summary>
    public bool UpdateSchemas { get; set; } = true;

    /// <summary>
    /// Writes the specified <see cref="PbirDefinition"/> to the file system.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public WriteOperationResults Write(PbirDefinition pbirDefinition)
    {
        if (pbirDefinition is null)
            throw new ArgumentNullException(nameof(pbirDefinition));

        using var context = new WriterContext(_fileSystem, Overwrite, _logger);
        // ReSharper disable AccessToDisposedClosure

        // version.json
        WriteDocument(pbirDefinition.VersionDocument, PbirNames.VersionMetadataFile, context);
        // report.json
        WriteDocument(pbirDefinition.Report, PbirNames.ReportFile, context);
        // reportExtensions.json
        WriteDocument(pbirDefinition.ReportExtensions, PbirNames.ReportExtensionFile, context);

        // bookmarks/
        WriteDocuments(pbirDefinition.Bookmarks, PbirNames.BookmarksFolder, context);

        // pages/
        WriteDocuments(pbirDefinition.Pages, PbirNames.PagesFolder, context, page =>
        {
            // visuals/
            WriteDocuments(page.Visuals, PbirNames.VisualsFolder, context, visual => WriteDocument(visual.MobileState, default, context));
        });

        // ReSharper restore AccessToDisposedClosure

        context.Commit();
        return context.GetResults();
    }

    private void WriteDocument<T>(T? document, RelativeFilePath? defaultPath, WriterContext context)
        where T : IPbirDocument
    {
        if (document is null)
            return;

        document.Path ??= defaultPath;
        var path = document.GetEffectivePath();

        if (UpdateSchemas)
            document.Schema = document.GetDefaultSchema();

        using var writer = _fileSystem.CreateTextWriter(path);
        _jsonSerializer.Serialize(writer, document);

        context.FilesWritten.Add(path);
        context.FilesDeleted.Remove(path);
    }

    private void WriteDocuments<TDocument, TMetadata>(IPbirDocumentCollection<TDocument, TMetadata> collection
            , RelativeFilePath? collectionPath
            , WriterContext context
            , Action<TDocument>? additionalDocAction = null)
        where TDocument : IPbirDocument
        where TMetadata : IPbirDocument
    {
        WriteDocument(collection.Metadata, default, context);

        foreach (var doc in collection)
        {
            WriteDocument(doc, default, context);
            additionalDocAction?.Invoke(doc);
        }
    }

    private void WriteDocuments<TDocument>(IPbirDocuments<TDocument> collection
        , RelativeFilePath? collectionPath
        , WriterContext context
        , Action<TDocument>? additionalDocAction = null)
        where TDocument : IPbirDocument
    {
        foreach (var doc in collection)
        {
            WriteDocument(doc, default, context);
            additionalDocAction?.Invoke(doc);
        }
    }

    private class WriterContext : IDisposable
    {
        private readonly IFabricItemFileSystem _fileSystem;
        private readonly ILogger _logger;
        private bool _committed;

        internal WriterContext(IFabricItemFileSystem fileSystem, bool overwrite, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            var baseDirectory = fileSystem.GetDirectoryInfo();
            if (!overwrite && baseDirectory.Exists && baseDirectory.EnumerateFiles().Any())
                throw new InvalidOperationException($"The directory: {baseDirectory.FullName} is not empty, and the Overwrite flag is not set.");

            baseDirectory.Create();
            // base path for all relative paths returned:
            _basePath = new Uri(baseDirectory.FullName.EnsureEndsInDirectorySeparator());

            // Make a list of all files present before the write operation
            // Used to determine which files (if any) to delete afterward (any files not updated)
            FilesDeleted = new HashSet<RelativeFilePath>(baseDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories)
                    .Select(fi => RelativeFilePath.Create(new Uri(fi.FullName), _basePath))
                    .Where(path => !path.Segments[0].StartsWith(".git")) // exclude all git system files
            );
        }

        private readonly Uri _basePath;

        internal HashSet<RelativeFilePath> FilesDeleted { get; }
        internal List<RelativeFilePath> FilesWritten { get; } = [];

        internal WriteOperationResults GetResults() => new(FilesWritten, FilesDeleted.ToList());

        /// <summary>
        /// Signals that the write operation was successful.
        /// </summary>
        internal void Commit()
        {
            _committed = true;
        }

        public void Dispose()
        {
            if (!_committed)
                return;

            foreach (var path in FilesDeleted.ToArray())
            {
                if (_fileSystem.FileExists(path))
                {
                    var fullPath = _basePath + path;
                    try
                    {
                        _fileSystem.DeleteFile(path);
                        _logger.LogDebug("Deleted file: {FullPath}", fullPath); // TODO static log method
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete file: {FullPath}", fullPath);
                    }
                }
                else
                {
                    FilesDeleted.Remove(path);
                }
            }

            // Remove empty dirs:
            foreach (var dir in _fileSystem.GetDirectoryInfo()
                         .EnumerateDirectories("*", SearchOption.AllDirectories)
                         .OrderBy(d => d.FullName) // If there's a series of empty folders, delete the top-most first
                         .ToArray())
            {
                if (dir.Exists && dir.EnumerateFiles("*.*", SearchOption.AllDirectories).FirstOrDefault() == null)
                {
                    try
                    {
                        dir.Delete(recursive: true); // Could be root of a series of empty folders
                        _logger.LogDebug("Deleted empty directory: {FullPath}", dir.FullName);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete directory: {FullPath}", dir.FullName);
                    }
                }
            }
        }
    }
}

/// <summary>
/// Summarizes a <see cref="PbirDefinitionWriter"/> write operation.
/// </summary>
public class WriteOperationResults(List<RelativeFilePath> filesWritten, List<RelativeFilePath> filesDeleted)
{

    /// <summary>
    /// Gets the relative paths of all files written during the operation.
    /// </summary>
    public IReadOnlyCollection<RelativeFilePath> FilesWritten { get; } = filesWritten.AsReadOnly();

    /// <summary>
    /// Gets the relative paths of all files deleted during the operation.
    /// </summary>
    public IReadOnlyCollection<RelativeFilePath> FilesDeleted { get; } = filesDeleted.AsReadOnly();

}
