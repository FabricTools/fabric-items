// Copyright (c) 2024 navidata.io Corp

namespace FabricTools.Items.IO;

/// <summary>
/// <see cref="IFabricItemFileSystem"/> extension methods.
/// </summary>
public static class FileSystemExtensions
{
    /// <summary>
    /// Attempts to create a <see cref="TextReader"/> for the specified <paramref name="relativePath"/>.
    /// Invokes the <paramref name="onSuccess"/> callback if the file is found, passing the reader and the relative path.
    /// Invokes the <paramref name="onNotFound"/> callback if the file does not exist.
    /// </summary>
    public static void TryReadFile(this IFabricItemFileSystem fileSystem, RelativeFilePath relativePath
        , Action<TextReader, RelativeFilePath> onSuccess
        , Action onNotFound)
    {
        TextReader reader;
        try
        {
            reader = fileSystem.CreateTextReader(relativePath);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            onNotFound();
            return;
        }
        using (reader)
        {
            onSuccess(reader, relativePath);
        }
    }
}
