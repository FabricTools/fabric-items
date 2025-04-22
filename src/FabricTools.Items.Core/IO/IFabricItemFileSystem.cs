// Copyright (c) 2024 navidata.io Corp

using System.IO.Abstractions;

namespace FabricTools.Items.IO;

/// <summary>
/// A file system abstraction used for reading and writing Fabric item definitions.
/// </summary>
public interface IFabricItemFileSystem
{
    /// <summary>
    /// The folder path.
    /// </summary>
    string BasePath { get; }

    /// <summary>
    /// Creates a new <see cref="TextReader"/> instance for the specified path, relative to the <see cref="BasePath"/>.
    /// </summary>
    /// <param name="relativePath">A path, relative to <see cref="BasePath"/>, pointing to an existing file.</param>
    /// <returns></returns>
    TextReader CreateTextReader(RelativeFilePath relativePath);

    /// <summary>
    /// Creates a new <see cref="TextWriter"/> instance for the specified path, relative to the <see cref="BasePath"/>.
    /// </summary>
    /// <param name="relativePath">A path, relative to <see cref="BasePath"/>, pointing to an existing file.</param>
    /// <returns></returns>
    TextWriter CreateTextWriter(RelativeFilePath relativePath);

    /// <summary>
    /// Gets the <see cref="IDirectoryInfo"/> for the folder at <see cref="BasePath"/>.
    /// </summary>
    IDirectoryInfo GetDirectoryInfo();

    /// <summary>
    /// Checks if the file at <param name="relativePath"/> exits.
    /// </summary>
    bool FileExists(RelativeFilePath relativePath);

    /// <summary>
    /// Deletes the file at <param name="relativePath"/>.
    /// </summary>
    void DeleteFile(RelativeFilePath relativePath);

    /// <summary>
    /// Enumerates all files matching the specified pattern, relative to the <see cref="BasePath"/> and the (optional) <paramref name="relativePath"/>.
    /// </summary>
    /// <param name="searchPattern">The search string to match against the names of files in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.</param>
    /// <param name="relativePath">An optional path, relative to <see cref="BasePath"/>, which determines the search location.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories. The default value is <see cref="SearchOption.TopDirectoryOnly"/>.</param>
    /// <returns>The respective <see cref="RelativeFilePath"/> for each matched file, relative to the specified <paramref name="relativePath"/>, if provided, or relative to <see cref="BasePath"/> otherwise.</returns>
    IEnumerable<RelativeFilePath> EnumerateFiles(string searchPattern, RelativeFilePath? relativePath = null, SearchOption searchOption = SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Enumerates all sub-folders, relative to the <see cref="BasePath"/> and the (optional) <paramref name="relativePath"/>.
    /// </summary>
    /// <param name="relativePath">An optional path, relative to <see cref="BasePath"/>, which determines the search location.</param>
    /// <returns>The respective <see cref="RelativeFilePath"/> for each sub-folder, relative to the specified <paramref name="relativePath"/>, if provided, or relative to <see cref="BasePath"/> otherwise.</returns>
    IEnumerable<RelativeFilePath> EnumerateFolders(RelativeFilePath? relativePath = null);
}
