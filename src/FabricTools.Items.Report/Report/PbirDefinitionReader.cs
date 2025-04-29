// Copyright (c) 2024 navidata.io Corp

using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using FabricTools.Items.IO;
using FabricTools.Items.Json;
using Newtonsoft.Json.Serialization;

namespace FabricTools.Items.Report;

/// <summary>
/// Reads PBIR definitions from a file system.
/// </summary>
public class PbirDefinitionReader
{
    private readonly ILogger _logger;
    private readonly IFabricItemFileSystem _fileSystem;
    private readonly JsonSerializer _jsonSerializer;

    /// <summary>
    /// Creates a new instance of <see cref="PbirDefinitionReader"/>.
    /// </summary>
    public PbirDefinitionReader(IFabricItemFileSystem fileSystem, ILoggerFactory? loggerFactory = null, bool enableTracing = false)
        : this(fileSystem
            , loggerFactory ?? NullLoggerFactory.Instance, 
            enableTracing ? (logger => new LoggingTraceWriter(logger)) : null)
    {
    }

    internal PbirDefinitionReader(IFabricItemFileSystem fileSystem, ILoggerFactory loggerFactory,
        Func<ILogger, ITraceWriter>? traceWriterFactory)
    {
        this._fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = loggerFactory.CreateLogger<PbirDefinitionReader>();

        _jsonSerializer = new JsonSerializer();
        if (traceWriterFactory is not null)
            _jsonSerializer.TraceWriter = traceWriterFactory(_logger);
        _jsonSerializer.Converters.Add(new NullableAnyOfJsonConverter());
    }

    /// <summary>
    /// Locates the file at the specified <paramref name="folderPath"/> plus <paramref name="filePath"/>, converts its content into an instance
    /// of <typeparamref name="T"/> and passes it to the <paramref name="onContentReady"/> callback method.
    /// Does nothing if the file is not found.
    /// </summary>
    internal void ReadFile<T>(RelativeFilePath folderPath, RelativeFilePath filePath, Action<T> onContentReady)
        where T : IPbirDocument, new()
    {
        var fullPath = folderPath + filePath;

        _fileSystem.TryReadFile(fullPath, (reader, _) => { 
            _logger.LogDebug("Reading file: {RelativeFilePath}", fullPath);

            using var jsonReader = new JsonTextReader(reader);

            var json = JObject.Load(jsonReader);
            try
            {
                var doc = json.ToObject<T>(_jsonSerializer)! // TODO Handle serialization errors??
                    .SetPath(filePath) // parent folders are inferred from parent/child hierarchy
                    .SetJson(json);

                onContentReady(doc);
            }
            catch (Exception e)
            {
                var ex = e;
                // TODO Throw a more specific exception
                throw;
            }

            _logger.LogInformation("Reading file completed: {RelativeFilePath}", fullPath);
        },
            message => _logger.LogWarning("File not found: {RelativeFilePath} ({Message})", fullPath, message)
        );
    }


    private void ReadFolder<TDocument, TMetadata>(RelativeFilePath folderPath, RelativeFilePath metadataFile,
        PbirDocumentCollection<TDocument, TMetadata> targetCollection,
        Func<RelativeFilePath, IEnumerable<TDocument>> createDocuments
    )
        where TDocument : class, IPbirDocument, new()
        where TMetadata : class, IPbirDocument, new()
    {
        TMetadata? metadata = null;
        ReadFile<TMetadata>(folderPath, metadataFile, meta => metadata = meta);

        targetCollection.Metadata = metadata;
        foreach (var document in createDocuments(folderPath))
        {
            targetCollection.Add(document);
        }
    }

    /// <summary>
    /// Reads each file matching the <paramref name="searchPattern"/> in the specified folder, and returns it as a <typeparamref name="TDocument"/>.
    /// </summary>
    private IEnumerable<TDocument> ReadFiles<TDocument>(RelativeFilePath folderPath, string searchPattern) where TDocument : IPbirDocument, new() =>
        _fileSystem.EnumerateFiles(searchPattern, folderPath)
            .Select(relativePath => {
                TDocument? document = default;
                ReadFile<TDocument>(folderPath, relativePath, content => document = content);
                return document;
            })
            .Where(d => d is not null)!;

    /// <summary>
    /// Reads a PBIR folder and returns a new <see cref="PbirDefinition"/> instance.
    /// </summary>
    public PbirDefinition Read()
    {
        var definition = new PbirDefinition { FileSystem = _fileSystem };

        // Version
        ReadFile<Definitions.VersionMetadata>(RelativeFilePath.Empty, PbirNames.VersionMetadataFile, version => definition.VersionDocument = version);

        // Report
        ReadFile<Definitions.Report>(RelativeFilePath.Empty, PbirNames.ReportFile, report => definition.Report = report);

        // ReportExtensions
        ReadFile<Definitions.ReportExtension>(RelativeFilePath.Empty, PbirNames.ReportExtensionFile, reportExt => definition.ReportExtensions = reportExt);

        // Bookmarks
        ReadFolder(PbirNames.BookmarksFolder, PbirNames.BookmarksMetadataFile,
            (PbirDocumentCollection<Definitions.Bookmark, Definitions.BookmarksMetadata>)definition.Bookmarks,
            folderPath => ReadFiles<Definitions.Bookmark>(folderPath, "*.bookmark.json")
        );

        IEnumerable<Definitions.Page> ReadPages(RelativeFilePath pagesPath)
        {
            foreach (var pagePath in _fileSystem.EnumerateFolders(pagesPath))
            {
                // Check /folderPath/{page}/page.json exists
                // Build ReportPage:
                // - Read page.json
                // - Read visuals/*/visual.json

                Definitions.Page? reportPage = null;

                ReadFile<Definitions.Page>(pagesPath, pagePath + PbirNames.PageFile, page => reportPage = page);

                if (reportPage is null) continue;

                // Visuals

                var visualsPath = pagesPath + pagePath + PbirNames.VisualsFolder;

                foreach (var visualPath in _fileSystem.EnumerateFolders(visualsPath))
                {
                    Definitions.VisualContainer? visualDoc = null;

                    ReadFile<Definitions.VisualContainer>(
                        visualsPath, visualPath + PbirNames.VisualFile,
                        visual => visualDoc = visual);

                    if (visualDoc is not null)
                    {
                        visualDoc.SetPath(visualPath + PbirNames.VisualFile);

                        ReadFile<Definitions.VisualContainerMobileState>(
                            visualsPath, visualPath + PbirNames.VisualMobileFile,
                            mobileState => visualDoc.MobileState = mobileState.SetPath(visualPath + PbirNames.VisualMobileFile));

                        reportPage.Visuals.Add(visualDoc);
                    }
                }
                
                yield return reportPage.SetPath(pagePath + PbirNames.PageFile);
            }
        }

        // Pages & Visuals
        ReadFolder(PbirNames.PagesFolder, PbirNames.PagesMetadataFile,
            (PbirDocumentCollection<Definitions.Page, Definitions.PagesMetadata>)definition.Pages,
            ReadPages
        );

        return definition;
    }

    internal class LoggingTraceWriter(ILogger logger) : ITraceWriter
    {
        private static LogLevel MapLevel(TraceLevel level) => level switch
        {
            TraceLevel.Error => LogLevel.Error,
            TraceLevel.Warning => LogLevel.Warning,
            TraceLevel.Info => LogLevel.Information,
            TraceLevel.Verbose => LogLevel.Debug,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };

        public void Trace(TraceLevel level, string message, Exception? ex)
        {
            var logLevel = MapLevel(level);
            if (!logger.IsEnabled(logLevel))
                return;

            if (ex is not null)
            {
                logger.Log(logLevel, ex, message);
            }
            else
            {
                logger.Log(logLevel, message);
            }
        }

        public TraceLevel LevelFilter => TraceLevel.Verbose;
    }
}