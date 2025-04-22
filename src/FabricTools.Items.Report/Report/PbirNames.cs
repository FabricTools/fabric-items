// Copyright (c) 2024 navidata.io Corp

namespace FabricTools.Items.Report;

/// <summary>
/// Defines well-known folder and file names used in the PBIR format.
/// </summary>
public static class PbirNames
{
    /// <summary>
    /// The file name of the PBIR schema version document.
    /// </summary>
    public const string VersionMetadataFile = "version.json";
    /// <summary>
    /// The file name of the report metadata document.
    /// </summary>
    public const string ReportFile = "report.json";
    /// <summary>
    /// The file name of the report extensions document.
    /// </summary>
    public const string ReportExtensionFile = "reportExtensions.json";
    /// <summary>
    /// The folder name containing all report bookmarks.
    /// </summary>
    public const string BookmarksFolder = "bookmarks";
    /// <summary>
    /// The file name of the bookmarks metadata document.
    /// </summary>
    public const string BookmarksMetadataFile = "bookmarks.json";
    /// <summary>
    /// The folder name containing all report pages.
    /// </summary>
    public const string PagesFolder = "pages";
    /// <summary>
    /// The file name of the pages metadata document.
    /// </summary>
    public const string PagesMetadataFile= "pages.json";
    /// <summary>
    /// The file name of the definition document for a single report page.
    /// </summary>
    public const string PageFile = "page.json";
    /// <summary>
    /// The folder name containing all report visuals for a given page.
    /// </summary>
    public const string VisualsFolder = "visuals";
    /// <summary>
    /// The file name of the definition document for a single visual.
    /// </summary>
    public const string VisualFile = "visual.json";
    /// <summary>
    /// The file name of the mobile layout document for a visual.
    /// </summary>
    public const string VisualMobileFile = "mobile.json";
}