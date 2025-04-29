// Copyright (c) 2024 navidata.io Corp

using Microsoft.Extensions.Logging.Abstractions;
using System.IO.Abstractions;
using System.Text;

namespace FabricTools.Items.IO;

/// <summary>
/// Implements <see cref="IFabricItemFileSystem"/> using <see cref="IFileSystem"/> as the backing file system.
/// </summary>
public class DefaultFileSystem : IFabricItemFileSystem
{
    private readonly IFileSystem _fileSystem;
    private readonly IDirectoryInfo _baseDirectory;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new <see cref="DefaultFileSystem"/> instance using the provided <see cref="IFileSystem"/> and the specified base path.
    /// </summary>
    public DefaultFileSystem(IFileSystem fileSystem, string basePath, ILoggerFactory? loggerFactory = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = loggerFactory?.CreateLogger<DefaultFileSystem>() ?? NullLoggerFactory.Instance.CreateLogger<DefaultFileSystem>();

        _baseDirectory = fileSystem.DirectoryInfo.New(basePath); // Ensures the path is valid
        BasePath = _baseDirectory.FullName.EnsureEndsInDirectorySeparator();
    }

    /// <inheritdocs/>
    public string BasePath { get; }

    /// <inheritdocs/>
    public IDirectoryInfo GetDirectoryInfo() => _fileSystem.DirectoryInfo.New(BasePath);

    /// <inheritdoc />
    public bool FileExists(RelativeFilePath relativePath) => _fileSystem.File.Exists(BasePath + relativePath);

    /// <inheritdoc />
    public void DeleteFile(RelativeFilePath relativePath) => _fileSystem.File.Delete(BasePath + relativePath);

    /// <inheritdocs/>
    public TextReader CreateTextReader(RelativeFilePath relativePath)
        // see: https://github.com/TestableIO/System.IO.Abstractions/issues/929#issuecomment-1367085547
        => new StreamReader(_fileSystem.FileStream.New(BasePath + relativePath, FileMode.Open), encoding: Encoding.UTF8);

    /// <inheritdocs/>
    public TextWriter CreateTextWriter(RelativeFilePath relativePath)
    {
        var file = _fileSystem.FileInfo.New(BasePath + relativePath);
        if (!file.Directory!.Exists)
            file.Directory!.Create();

        return new StreamWriter(file.Create(), encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false /* Important to write w/o BOM */)); 
    }

    /// <inheritdocs/>
    public IEnumerable<RelativeFilePath> EnumerateFiles(string pattern, RelativeFilePath? relativePath = null, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        var searchPath = relativePath.HasValue
            ? BasePath + relativePath.Value
            : BasePath;
        var searchPathUri = new Uri(searchPath.EnsureEndsInDirectorySeparator());

        try
        {
            return _fileSystem.Directory
                .EnumerateFiles(searchPath, pattern, searchOption)
                .Select(fullPath => RelativeFilePath.Create(new Uri(fullPath), searchPathUri));
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
    }

    /// <inheritdocs/>
    public IEnumerable<RelativeFilePath> EnumerateFolders(RelativeFilePath? relativePath = null)
    {
        var searchPath = relativePath.HasValue
            ? BasePath + relativePath.Value
            : BasePath;
        var searchPathUri = new Uri(searchPath.EnsureEndsInDirectorySeparator());

        try
        {
            return _fileSystem.Directory
                .EnumerateDirectories(searchPath)
                .Select(fullPath => RelativeFilePath.Create(new Uri(fullPath), searchPathUri));
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
    }
}
