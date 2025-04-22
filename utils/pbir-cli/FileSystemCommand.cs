// Copyright (c) 2024 navidata.io Corp

namespace FabricTools.Items.Report.Cli;

internal abstract class FileSystemCommand<TSettings>(IFileSystem? fileSystem = null)
    : Command<TSettings>
    where TSettings : CommandSettings
{
    protected readonly IFileSystem FileSystem = fileSystem ?? new FileSystem();
}